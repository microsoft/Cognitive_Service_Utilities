//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Evaluation.Interfaces;
using AIPlatform.TestingFramework.Evaluation.STT;
using AIPlatform.TestingFramework.Evaluation.Translation;
using AIPlatform.TestingFramework.Pipeline.Configuration;
using AIPlatform.TestingFramework.PostProcessingSTT;
using AIPlatform.TestingFramework.STT;
using AIPlatform.TestingFramework.SubtitlesGeneration;
using AIPlatform.TestingFramework.Translation;
using AIPlatform.TestingFramework.TTS;
using AIPlatform.TestingFramework.TTSPreProcessing;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions;
using AIPlatform.TestingFramework.Utilities.Orchestrator;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework
{
    public class TestingFrameworkOrchestrator
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> appLogger;
        private readonly ISpeechToText speechToText;
        private readonly IPostProcessSTT postProcessSTT;
        private readonly ISpeechCorrectnessEvaluator speechCorrectnessEvaluator;
        private readonly ITranslator translator;
        private readonly ITranslationCorrectnessEvaluator translationCorrectnessEvaluator;
        private readonly ITextToSpeech textToSpeech;
        private readonly IPreProcessTTS preprocessTTS;
        private readonly ISubtitlesWriting subtitlesWriting;
        private readonly OrchestratorHelper orchestratorHelper;

        private const string FirstPipelineStepInputName = "PipelineStepInput";
        private readonly Dictionary<string, string> outputDictionary = new Dictionary<string, string>();
        private IDurableOrchestrationContext context;

        public TestingFrameworkOrchestrator(
            IOrchestratorLogger<TestingFrameworkOrchestrator> logger,
            ISpeechToText speechToText,
            IPostProcessSTT postProcessSTT,
            ISpeechCorrectnessEvaluator speechCorrectnessEvaluator,
            ITranslator translator,
            ITranslationCorrectnessEvaluator translationCorrectnessEvaluator,
            IPreProcessTTS preprocessTTS,
            ITextToSpeech textToSpeech,
            ISubtitlesWriting subtitlesWriting,
            IStorageManager storageManager)
        {
            appLogger = logger;
            this.speechToText = speechToText;
            this.postProcessSTT = postProcessSTT;
            this.speechCorrectnessEvaluator = speechCorrectnessEvaluator;
            this.textToSpeech = textToSpeech;
            this.preprocessTTS = preprocessTTS;
            this.translator = translator;
            this.translationCorrectnessEvaluator = translationCorrectnessEvaluator;
            this.subtitlesWriting = subtitlesWriting;
            orchestratorHelper = new OrchestratorHelper(logger, storageManager);
        }

        [FunctionName("TestingFrameworkOrchestrator")]
        public async Task<Dictionary<string, string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context)
        {
            this.context = context;
            //var outputs = new List<string>();
            var logger = context.CreateReplaySafeLogger(appLogger);
            logger.LogInformation("Starting Orchestrator...");

            // parse input
            string payload = context.GetInput<string>();
            PostBody postBody = JsonConvert.DeserializeObject<PostBody>(payload);

            List<PipelineStep> pipelineSteps = postBody.ExecutionPipeline.PipelineSteps.OrderBy(s => s.StepOrder).ToList();

            orchestratorHelper.ValidatePipeline(pipelineSteps);

            pipelineSteps = orchestratorHelper.UpdateDatasetInPipelineSteps(pipelineSteps, postBody.Dataset);

            outputDictionary.Add(FirstPipelineStepInputName, postBody.Input);
            string previousPipelineStepId = FirstPipelineStepInputName;

            // execute the pipeline
            foreach (var pipelineStep in pipelineSteps)
            {
                List<string> inputs = await GetPipelineInputsAsync(pipelineStep, previousPipelineStepId, postBody.Dataset);

                var executionMode = pipelineStep.ExecutionMode;

                if (executionMode == StepExecutionModeEnum.Parallel)
                {
                    var tasks = new List<Task<Dictionary<string, string>>>();

                    foreach (var parallelStep in pipelineStep.PipelineSteps)
                    {
                        inputs = await GetPipelineInputsAsync(parallelStep, previousPipelineStepId, postBody.Dataset);
                        tasks.Add(RunAtomicPipelineStep(parallelStep, inputs, postBody.Dataset));
                    }

                    foreach (Dictionary<string, string> result in await Task.WhenAll(tasks))
                    {
                        result.ToList().ForEach(pair => outputDictionary.Add(pair.Key, pair.Value));
                    }
                }
                else
                {
                    Dictionary<string, string> result = await RunAtomicPipelineStep(pipelineStep, inputs, postBody.Dataset);
                    result.ToList().ForEach(pair => outputDictionary.Add(pair.Key, pair.Value));
                }

                previousPipelineStepId = pipelineStep.StepID;
            }

            return outputDictionary;
        }

        private async Task<List<string>> GetPipelineInputsAsync(PipelineStep pipelineStep, string previousStepId, Dictionary<string, string> dataset)
        {
            List<string> inputs = new List<string>();

            if (pipelineStep.Inputs?.Count == 0)
            {
                //get all outputs from the previous step
                foreach (string key in outputDictionary.Keys)
                {
                    if (key.StartsWith($"{previousStepId}"))
                    {
                        inputs.Add(outputDictionary[key]);
                    }
                }
            }

            if (pipelineStep.Inputs?.Count > 0)
            {
                foreach (var stepInputKey in pipelineStep.Inputs)
                {
                    string inputContent;
                    if (stepInputKey.Contains("Dataset."))
                    {
                        string keyName = stepInputKey.Replace("Dataset.", "");
                        string storagePath = dataset[keyName];
                        inputContent = await context.CallActivityAsync<string>("GetPipelineInputFromStorage", storagePath);
                    }
                    else
                    {
                        inputContent = orchestratorHelper.GetPipelineInputFromOutputDictionary(stepInputKey, outputDictionary);
                    }

                    inputs.Add(inputContent);
                }
            }

            return inputs;
        }

        private async Task<Dictionary<string, string>> RunAtomicPipelineStep(PipelineStep pipelineStep, List<string> inputs, Dictionary<string, string> dataset)
        {
            Dictionary<string, string> stepOutputDict = new Dictionary<string, string>();

            string stepResult = await ExecutePipelineStep(pipelineStep, inputs);

            //write output of the pipelineStep to outputDictionary
            List<(string, string)> parsedOutputs = orchestratorHelper.ParsePipelineStepOutput(stepResult, pipelineStep);

            foreach (var output in parsedOutputs)
            {
                stepOutputDict.Add(output.Item1, output.Item2);
            }

            //write output of the pipelineStep to blobstorage
            List<string> datasetKeys = pipelineStep.Outputs?.Where(k => k.StartsWith("Dataset.")).ToList();
            if (datasetKeys.Any())
            {
                Dictionary<string, string> writeToStorageDict = new Dictionary<string, string>();

                for (int i = 0; i < datasetKeys.Count; i++)
                {
                    string key = datasetKeys[i].Replace("Dataset.", "");
                    string filePath = dataset[key];
                    string fileContent = parsedOutputs.ElementAt(i).Item2;
                    writeToStorageDict.Add(filePath, fileContent);
                }

                //write outputs to storage
                Dictionary<string, string> stgOutputs = await context.CallActivityAsync<Dictionary<string, string>>("WritePipelineOutputToStorage", writeToStorageDict);

                //include file absolute paths in the output dictionary
                stgOutputs.ToList().ForEach(pair => stepOutputDict.Add(pair.Key, pair.Value));
            }

            return stepOutputDict;
        }

        private Task<string> ExecutePipelineStep(PipelineStep pipelineStep, List<string> inputs)
        {
            var stepConfigJson = JsonConvert.SerializeObject(pipelineStep.StepConfig);

            if (string.IsNullOrEmpty(stepConfigJson) || stepConfigJson == "null")
            {
                throw new ArgumentNullException(nameof(stepConfigJson));
            }

            if (inputs == null)
            {
                throw new ArgumentNullException(nameof(inputs));
            }

            string input = string.Empty;
            if (inputs.Count == 1)
            {
                input = inputs[0];
            }

            Task<string> t;
            switch (pipelineStep.StepName)
            {
                case StepNameEnum.SpeechToText:
                    SpeechConfiguration speechConfig = UpdateSpeechConfigFromAppSettings(stepConfigJson);

                    var speechInput = new SpeechInput(speechConfig, input);
                    t = context.CallActivityAsync<string>(pipelineStep.StepName.ToString(), speechInput);
                    break;

                case StepNameEnum.PostProcessSTT:
                    PostProcessSTTConfiguration postProcessSTTConfig = DeserializePostProcessSTTConfig(stepConfigJson);
                    List<SpeechOutputSegment> postProcessSTTInputCollection = JsonConvert.DeserializeObject<List<SpeechOutputSegment>>(input);
                    var postProcessSTTInput = new PostProcessSTTInput(postProcessSTTConfig, postProcessSTTInputCollection);
                    t = context.CallActivityAsync<string>(pipelineStep.StepName.ToString(), postProcessSTTInput);
                    break;

                case StepNameEnum.SpeechCorrectnessEvaluation:
                    SpeechCorrectnessConfiguration speechCorrectnessConfig = DeserializeSpeechCorrectnessConfig(stepConfigJson);
                    List<SpeechOutputSegment> speechCorrectnessInputCollection = JsonConvert.DeserializeObject<List<SpeechOutputSegment>>(input);
                    var speechCorrectnessInput = new SpeechCorrectnessInput(speechCorrectnessConfig, speechCorrectnessInputCollection);
                    t = context.CallActivityAsync<string>(pipelineStep.StepName.ToString(), speechCorrectnessInput);
                    break;

                case StepNameEnum.Translation:
                    TranslatorConfiguration translatorConfig = UpdateTranslatorConfigFromAppSettings(stepConfigJson);
                    TranslatorInput translatorInput;

                    if (translatorConfig.IsInputSegmented)
                    {
                        var segmentedInput = JsonConvert.DeserializeObject<ICollection<TranslatorInputSegment>>(input);
                        translatorInput = new TranslatorInput(translatorConfig, segmentedInput);
                    }
                    else
                    {
                        var segmentedInput = new List<TranslatorInputSegment>() { new TranslatorInputSegment(input) };
                        translatorInput = new TranslatorInput(translatorConfig, segmentedInput);
                    }

                    t = context.CallActivityAsync<string>(pipelineStep.StepName.ToString(), translatorInput);
                    break;

                case StepNameEnum.TranslationCorrectnessEvaluation:
                    TranslationCorrectnessConfiguration translationCorrectnessConfig = DeserializeTranslationCorrectnessConfig(stepConfigJson);
                    translationCorrectnessConfig.TranslatorConfiguration.ServiceConfiguration = GetTranslationServicesConfiguration();
                    List<SpeechOutputSegment> transcriptionCorrectnessInputCollection = JsonConvert.DeserializeObject<List<SpeechOutputSegment>>(inputs[0]);
                    List<TranslatorOutputSegment> translationCorrectnessInputCollection = JsonConvert.DeserializeObject<List<TranslatorOutputSegment>>(inputs[1]);
                    var translationCorrectnessInput = new TranslationCorrectnessInput(translationCorrectnessConfig, transcriptionCorrectnessInputCollection, translationCorrectnessInputCollection);
                    t = context.CallActivityAsync<string>(pipelineStep.StepName.ToString(), translationCorrectnessInput);
                    break;

                case StepNameEnum.SubtitlesWriting:
                    var subtitlesWritingConfiguration = UpdateSubtitlesWritingConfigFromAppSettings(stepConfigJson);

                    //var subtitlesInputs = JsonConvert.DeserializeObject<List<string>>(inputs);

                    var subtitlesWritingInputs = JsonConvert.DeserializeObject<List<SubtitlesWritingInput>>(inputs[0]);
                    var humanInterventions = JsonConvert.DeserializeObject<List<SpeechCorrectnessOutputSegment>>(inputs[1]);

                    for (int i = 0; i < subtitlesWritingInputs.Count; i++)
                    {
                        if (subtitlesWritingInputs[i].SegmentID != humanInterventions[i].SegmentID)
                        {
                            throw new ArgumentException($"Segments at position {i} don't have the same SegmentID - {subtitlesWritingInputs[i].SegmentID} & {humanInterventions[i].SegmentID}");
                        }

                        subtitlesWritingInputs[i].HumanIntervention = humanInterventions[i].InterventionNeeded;
                        subtitlesWritingInputs[i].HumanInterventionReasons = humanInterventions[i].InterventionReasons;
                    }

                    SubtitlesWritingConfigInput configInput = new SubtitlesWritingConfigInput(subtitlesWritingInputs, subtitlesWritingConfiguration);
                    t = context.CallActivityAsync<string>(pipelineStep.StepName.ToString(), configInput);
                    break;
                case StepNameEnum.PreProcessTTS:
                    var preProcessTTSConfig = UpdatePreprocessTTSInputFromAppSettings(stepConfigJson);

                    var segmentDetailsList = JsonConvert.DeserializeObject<List<PreProcessTTSInput>>(inputs[0]);
                    var translatedSegmentsList = JsonConvert.DeserializeObject<List<TranslatorOutputSegment>>(inputs[1]);

                    for (int i = 0; i < segmentDetailsList.Count; i++)
                    {
                        if (segmentDetailsList[i].SegmentID != translatedSegmentsList[i].SegmentID)
                        {
                            throw new ArgumentException($"Segments at position {i} are not the same - {segmentDetailsList[i].SegmentID} & {translatedSegmentsList[i].SegmentID}");
                        }

                        string speakerId = string.IsNullOrEmpty(segmentDetailsList[i].IdentifiedSpeaker) ? "Default" : segmentDetailsList[i].IdentifiedSpeaker;
                        //get voiceInfo my locale
                        var locale = segmentDetailsList[i].TargetLocale;

                        bool localeFound = preProcessTTSConfig.VoiceMapping.TryGetValue($"{speakerId}_{locale}", out var voiceMapping);
                        if (!localeFound)
                        {
                            appLogger.LogError($"No matching voice information found for speaker and locale combination: {speakerId}_{locale}");
                            throw new Exception($"No matching voice information found for speaker and locale combination: {speakerId}_{locale}");
                        }
                        //voiceMapping.Locale = locale;
                        segmentDetailsList[i].VoiceInfo = voiceMapping;
                        segmentDetailsList[i].TranslatedText = translatedSegmentsList[i].TranslatedText;
                        segmentDetailsList[i].PreProcessingStepConfig = preProcessTTSConfig;
                    }

                    t = context.CallActivityAsync<string>(pipelineStep.StepName.ToString(), segmentDetailsList);
                    break;

                case StepNameEnum.TextToSpeech:
                    TextToSpeechConfiguration textToSpeechConfiguration = UpdateTextToSpeechConfigFromAppSettings(stepConfigJson);

                    var ssmlInput = new TextToSpeechInput(textToSpeechConfiguration, input);
                    t = context.CallActivityAsync<string>(pipelineStep.StepName.ToString(), ssmlInput);
                    break;

                default:
                    throw new Exception($"{pipelineStep.StepName} is not implemented");
            }

            return t;
        }

        //Activity function for fetching inputs
        [FunctionName("GetPipelineInputFromStorage")]
        public async Task<string> GetPipelineInputFromStorage([ActivityTrigger] string storagePath) =>
            await orchestratorHelper.GetPipelineInputFromStorage(storagePath);

        //Activity function for writing outputs to storage
        [FunctionName("WritePipelineOutputToStorage")]
        public async Task<Dictionary<string, string>> WritePipelineOutputToStorage([ActivityTrigger] Dictionary<string, string> writeToStorageDict) =>
            await orchestratorHelper.WriteTextFilesToStorageAsync(writeToStorageDict);

        [FunctionName("SubtitlesWriting")]
        public string WriteCaptions([ActivityTrigger] SubtitlesWritingConfigInput captionsWritingConfigInput) => this.subtitlesWriting.WriteSubtitles(captionsWritingConfigInput);

        //Activity function for transcription
        [FunctionName("SpeechToText")]
        public async Task<string> DoTranscriptionAsync([ActivityTrigger] SpeechInput speechInput) =>
            await speechToText.DoTranscription(speechInput);

        [FunctionName("PreProcessTTS")]
        public string DoPreprocessTTSAsync([ActivityTrigger] List<PreProcessTTSInput> preprocessTTSInput) => preprocessTTS.DoTTSPreProcessing(preprocessTTSInput);

        //Activity function for processing the speech-to-text output for other steps to consume
        [FunctionName("PostProcessSTT")]
        public string DoPostProcessSTT([ActivityTrigger] PostProcessSTTInput postprocessSTTInput) => postProcessSTT.DoSpeechToTextPostProcessing(postprocessSTTInput);

        //Activity function for evaluating the correctness of the speech-to-text output
        [FunctionName("SpeechCorrectnessEvaluation")]
        public string DoSpeechCorrectnessEvaluation([ActivityTrigger] SpeechCorrectnessInput speechCorrectnessInput) => speechCorrectnessEvaluator.EvaluateCorrectness(speechCorrectnessInput);

        //Activity function for translation
        [FunctionName("Translation")]
        public async Task<string> DoTranslation([ActivityTrigger] TranslatorInput translatorInput) => await translator.DoTranslation(translatorInput);

        //Activity function for evaluating the correctness of the translation output
        [FunctionName("TranslationCorrectnessEvaluation")]
        public async Task<string> DoTranslationCorrectnessEvaluation([ActivityTrigger] TranslationCorrectnessInput translationCorrectnessInput) => await translationCorrectnessEvaluator.EvaluateCorrectnessAsync(translationCorrectnessInput);

        //Activity function for Text to Speech
        [FunctionName("TextToSpeech")]
        public async Task<string> GenerateSpeechAudio([ActivityTrigger] TextToSpeechInput ttsInput) => await textToSpeech.GenerateAudioAsync(ttsInput);

        [FunctionName("TestingFrameworkOrchestrator_HttpStart")]
        public async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter)
        {
            string payload = await req.Content.ReadAsStringAsync();
            appLogger.LogInformation($"HttpTrigger Payload:\n{payload}.");

            string instanceId = await starter.StartNewAsync<string>("TestingFrameworkOrchestrator", payload);
            appLogger.LogInformation($"Started orchestration with ID = '{instanceId}'.");
            appLogger.AddGlobalProperty("instance_id", instanceId);

            return starter.CreateCheckStatusResponse(req, instanceId, false);
        }

        private TextToSpeechConfiguration UpdateTextToSpeechConfigFromAppSettings(string configJson)
        {
            if (string.IsNullOrEmpty(configJson))
            {
                appLogger.LogError($"{nameof(configJson)} is null or empty");
                throw new ArgumentNullException(nameof(configJson));
            }

            var textToSpeechConfig = JsonConvert.DeserializeObject<TextToSpeechConfiguration>(configJson);
            textToSpeechConfig.ServiceConfiguration = GetSpeechServicesConfiguration();

            return textToSpeechConfig;
        }

        private SubtitlesWritingConfiguration UpdateSubtitlesWritingConfigFromAppSettings(string stepConfig)
        {
            if (string.IsNullOrEmpty(stepConfig))
            {
                appLogger.LogError($"{nameof(stepConfig)} is null or empty");
                throw new ArgumentNullException(nameof(stepConfig));
            }

            var captionsWritingConfig = JsonConvert.DeserializeObject<SubtitlesWritingConfiguration>(stepConfig);
            return captionsWritingConfig;
        }

        private TTSPreProcessingConfiguration UpdatePreprocessTTSInputFromAppSettings(string stepConfig)
        {
            if (string.IsNullOrEmpty(stepConfig))
            {
                appLogger.LogError($"{nameof(stepConfig)} is null or empty");
                throw new ArgumentNullException(nameof(stepConfig));
            }

            var ttsPreProcessingConfig = JsonConvert.DeserializeObject<TTSPreProcessingConfiguration>(stepConfig);
            ttsPreProcessingConfig.ServiceConfiguration = GetSpeechServicesConfiguration();

            return ttsPreProcessingConfig;
        }

        private SpeechConfiguration UpdateSpeechConfigFromAppSettings(string configJson)
        {
            if (string.IsNullOrEmpty(configJson))
            {
                appLogger.LogError($"{nameof(configJson)} is null or empty");
                throw new ArgumentNullException(nameof(configJson));
            }

            var speechConfig = JsonConvert.DeserializeObject<SpeechConfiguration>(configJson);

            speechConfig.ServiceConfiguration = GetSpeechServicesConfiguration();
            speechConfig.EndpointId = Environment.GetEnvironmentVariable("SpeechConfiguration_EndpointId");

            return speechConfig;
        }

        public TranslatorConfiguration UpdateTranslatorConfigFromAppSettings(string configJson)
        {
            if (string.IsNullOrEmpty(configJson))
            {
                appLogger.LogError($"{nameof(configJson)} is null or empty");
                throw new ArgumentNullException(nameof(configJson));
            }

            var translatorConfig = JsonConvert.DeserializeObject<TranslatorConfiguration>(configJson);

            translatorConfig.ServiceConfiguration = GetTranslationServicesConfiguration();

            return translatorConfig;
        }

        private PostProcessSTTConfiguration DeserializePostProcessSTTConfig(string configJson)
        {
            if (string.IsNullOrEmpty(configJson))
            {
                appLogger.LogError($"{nameof(configJson)} is null or empty");
                throw new ArgumentNullException(nameof(configJson));
            }

            var postProcessSTTConfig = JsonConvert.DeserializeObject<PostProcessSTTConfiguration>(configJson);

            return postProcessSTTConfig;
        }

        private SpeechCorrectnessConfiguration DeserializeSpeechCorrectnessConfig(string configJson)
        {
            if (string.IsNullOrEmpty(configJson))
            {
                appLogger.LogError($"{nameof(configJson)} is null or empty");
                throw new ArgumentNullException(nameof(configJson));
            }

            var speechCorrectnessConfig = JsonConvert.DeserializeObject<SpeechCorrectnessConfiguration>(configJson);

            return speechCorrectnessConfig;
        }

        private TranslationCorrectnessConfiguration DeserializeTranslationCorrectnessConfig(string configJson)
        {
            if (string.IsNullOrEmpty(configJson))
            {
                appLogger.LogError($"{nameof(configJson)} is null or empty");
                throw new ArgumentNullException(nameof(configJson));
            }

            var translationCorrectnessConfig = JsonConvert.DeserializeObject<TranslationCorrectnessConfiguration>(configJson);

            return translationCorrectnessConfig;
        }

        private CognitiveServiceConfiguration GetSpeechServicesConfiguration()
        {
            string region = Environment.GetEnvironmentVariable("SpeechConfiguration_Region");
            string subscriptionKey = Environment.GetEnvironmentVariable("SpeechConfiguration_SubscriptionKey");

            return new CognitiveServiceConfiguration
            {
                Region = region,
                SubscriptionKey = subscriptionKey
            };
        }

        private CognitiveServiceConfiguration GetTranslationServicesConfiguration()
        {
            string region = Environment.GetEnvironmentVariable("TranslationConfiguration_Region");
            string subscriptionKey = Environment.GetEnvironmentVariable("TranslationConfiguration_SubscriptionKey");

            return new CognitiveServiceConfiguration
            {
                Region = region,
                SubscriptionKey = subscriptionKey
            };
        }
    }
}
