using AIPlatform.TestingFramework;
using AIPlatform.TestingFramework.Evaluation.Interfaces;
using AIPlatform.TestingFramework.Pipeline.Configuration;
using AIPlatform.TestingFramework.PostProcessingSTT;
using AIPlatform.TestingFramework.STT;
using AIPlatform.TestingFramework.SubtitlesGeneration;
using AIPlatform.TestingFramework.Translation;
using AIPlatform.TestingFramework.TTS;
using AIPlatform.TestingFramework.TTSPreProcessing;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Linq;

namespace AIPlatform.EvaluationFramework.Test.Unit
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class OrchestratorTest
    {
        PostBody seriesPostBody;
        PostBody parallelPostBody;
        TestingFrameworkOrchestrator durableFunction;
        private const string FirstPipelineStepInputName = "PipelineStepInput";

        [TestInitialize]
        public void Initialize()
        {
            var sttStep = new PipelineStep
            {
                StepOrder = 1,
                StepName = StepNameEnum.SpeechToText,
                StepID = "Step1",
                StepConfig = JObject.FromObject(new SpeechConfiguration()),
            };

            var translationStep = new PipelineStep
            {
                StepOrder = 2,
                StepName = StepNameEnum.Translation,
                StepID = "Step2",
                StepConfig = JObject.FromObject(new TranslatorConfiguration())
            };

            this.seriesPostBody = new PostBody
            {
                Input = "I want 10 50 dollar tickets for this weekends game",
                ExpectedOutput = "TestOutput_NotUsed",
                ExecutionPipeline = new PipelineInstance()
            };
            seriesPostBody.ExecutionPipeline.Name = "TestPipeline";
            seriesPostBody.ExecutionPipeline.PipelineSteps = new List<PipelineStep> { sttStep, translationStep };

            //creare parallel pipeline post body
            var parallelPipeLineStep = new PipelineStep
            {
                StepOrder = 2,
                StepID = StepNameEnum.ParallelStep.ToString(),
                ExecutionMode = StepExecutionModeEnum.Parallel,
                PipelineSteps = new List<PipelineStep>() { sttStep, translationStep }
            };

            this.parallelPostBody = new PostBody
            {
                Input = "I want 10 50 dollar tickets for this weekends game",
                ExpectedOutput = "TestOutput_NotUsed",
                ExecutionPipeline = new PipelineInstance()
            };
            parallelPostBody.ExecutionPipeline.Name = "TestPipeline";
            parallelPostBody.ExecutionPipeline.PipelineSteps = new List<PipelineStep> { parallelPipeLineStep };

            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var speechServiceMock = new Mock<ISpeechToText>();
            var postProcessSTTMock = new Mock<IPostProcessSTT>();
            var speechCorrectnessEvaluationMock = new Mock<ISpeechCorrectnessEvaluator>();
            var translatorMock = new Mock<ITranslator>();
            var translatorCorrectnessEvaluationMock = new Mock<ITranslationCorrectnessEvaluator>();
            var ttsPreProcessMock = new Mock<IPreProcessTTS>();
            var ttsServiceMock = new Mock<ITextToSpeech>();
            var storageMgrMock = new Mock<IStorageManager>();
            var subtitlesWritingMock = new Mock<ISubtitlesWriting>();

            durableFunction = new TestingFrameworkOrchestrator(
                loggerMock.Object,
                speechServiceMock.Object,
                postProcessSTTMock.Object,
                speechCorrectnessEvaluationMock.Object,
                translatorMock.Object,
                translatorCorrectnessEvaluationMock.Object,
                ttsPreProcessMock.Object,
                ttsServiceMock.Object,
                subtitlesWritingMock.Object,
                storageMgrMock.Object);
        }

        [TestMethod]
        public async Task HttpStart_Returns_ProperHeaderAsync()
        {
            //Setup
            const string functionName = "TestingFrameworkOrchestrator";
            const string instanceId = "7E467BDB-213F-407A-B86A-1954053D3C24";

            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var logger = loggerMock.Object;

            var durableClientMock = new Mock<IDurableClient>();

            durableClientMock.
                Setup(x => x.StartNewAsync<string>(functionName, It.IsAny<string>())).
                ReturnsAsync(instanceId);

            durableClientMock
                .Setup(x => x.CreateCheckStatusResponse(It.IsAny<HttpRequestMessage>(), instanceId, false))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(string.Empty),
                    Headers =
                    {
                        RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(10))
                    }
                });

            //RunTest
            var result = await durableFunction.HttpStart(
                new HttpRequestMessage()
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json"),
                    RequestUri = new Uri("http://localhost:7071/api/"),
                },
                durableClientMock.Object);

            // Validate that output is not null
            Assert.IsNotNull(result.Headers.RetryAfter);

            // Validate output's Retry-After header value
            Assert.AreEqual(TimeSpan.FromSeconds(10), result.Headers.RetryAfter.Delta);
        }

        [TestMethod]
        public async Task RunOrchestrator_Executes_Pipeline_InSeries_And_Returns_ProperOutput()
        {
            // Pipeline has one stt and one translations run in series. 
            //Setup

            var jsonPayload = JsonConvert.SerializeObject(this.seriesPostBody);

            var durableOrchestrationContextMock = new Mock<IDurableOrchestrationContext>();
            durableOrchestrationContextMock
                .Setup(s => s.GetInput<string>()).Returns(jsonPayload);

            string transcriptionResult = "This is transcription result";
            string translatedResult = "This is translated result";

            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>(StepNameEnum.SpeechToText.ToString(), It.IsAny<SpeechInput>())).ReturnsAsync(transcriptionResult);
            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>(StepNameEnum.Translation.ToString(), It.IsAny<TranslatorInput>())).ReturnsAsync(translatedResult);

            //Execute Test
            var result = await durableFunction.RunOrchestrator(durableOrchestrationContextMock.Object);

            //assert output has returned values in order
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(result[FirstPipelineStepInputName], this.seriesPostBody.Input);

            string transcriptionStepID = this.seriesPostBody.ExecutionPipeline.PipelineSteps[0].StepID;
            Assert.IsTrue(result.ContainsKey(transcriptionStepID));
            Assert.AreEqual(result[transcriptionStepID], transcriptionResult);

            string translationStepID = this.seriesPostBody.ExecutionPipeline.PipelineSteps[1].StepID;
            Assert.IsTrue(result.ContainsKey(translationStepID));
            Assert.AreEqual(result[translationStepID], translatedResult);

            //asert that right method calls were made
            durableOrchestrationContextMock.Verify(mock => mock.GetInput<string>(), Times.Once);
            durableOrchestrationContextMock.Verify(mock => mock.CallActivityAsync<string>(StepNameEnum.SpeechToText.ToString(), It.IsAny<SpeechInput>()), Times.Once);
            durableOrchestrationContextMock.Verify(mock => mock.CallActivityAsync<string>(StepNameEnum.Translation.ToString(), It.IsAny<TranslatorInput>()), Times.Once);

        }

        [TestMethod]
        public async Task RunOrchestrator_Executes_ParallePipelineSteps()
        {
            //Setup
            var jsonPayload = JsonConvert.SerializeObject(this.parallelPostBody);

            var durableOrchestrationContextMock = new Mock<IDurableOrchestrationContext>();
            durableOrchestrationContextMock
                .Setup(s => s.GetInput<string>()).Returns(jsonPayload);

            string transcriptionResult = "This is transcription result";
            string translatedResult = "This is translated result";

            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>(StepNameEnum.SpeechToText.ToString(), It.IsAny<SpeechInput>())).ReturnsAsync(transcriptionResult);
            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>(StepNameEnum.Translation.ToString(), It.IsAny<TranslatorInput>())).ReturnsAsync(translatedResult);

            //Execute Test
            var result = await durableFunction.RunOrchestrator(durableOrchestrationContextMock.Object);

            //assert output has return values
            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(result[FirstPipelineStepInputName], this.seriesPostBody.Input);

            string transcriptionStepID = this.seriesPostBody.ExecutionPipeline.PipelineSteps[0].StepID;
            Assert.IsTrue(result.ContainsKey(transcriptionStepID));
            Assert.AreEqual(result[transcriptionStepID], transcriptionResult);

            string translationStepID = this.seriesPostBody.ExecutionPipeline.PipelineSteps[1].StepID;
            Assert.IsTrue(result.ContainsKey(translationStepID));
            Assert.AreEqual(result[translationStepID], translatedResult);

            //asert that right method calls were made
            durableOrchestrationContextMock.Verify(mock => mock.GetInput<string>(), Times.Once);
            durableOrchestrationContextMock.Verify(mock => mock.CallActivityAsync<string>(StepNameEnum.SpeechToText.ToString(), It.IsAny<SpeechInput>()), Times.Once);
            durableOrchestrationContextMock.Verify(mock => mock.CallActivityAsync<string>(StepNameEnum.Translation.ToString(), It.IsAny<TranslatorInput>()), Times.Once);

        }
        
        [TestMethod]
        public async Task RunOrchestrator_Handles_Dataset_AsInputs_During_Serial_Execution()
        {
            //setup pipeline and mocks
            string sttStepId = "SpeechToText";
            string translationStepId = "Translation";
            string sttResultKey = "RecognitionResult";
            string transcriptionResult = "This is transcription result";
            string translatedResult = "This is translated result";

            var sttStep = new PipelineStep
            {
                StepOrder = 1,
                StepName = StepNameEnum.SpeechToText,
                StepID = sttStepId,
                StepConfig = JObject.FromObject(new SpeechConfiguration()),
                Outputs = new List<string> { sttResultKey, "Dataset.STT_RecognitionResult" }
            };

            var translationStep = new PipelineStep
            {
                StepOrder = 2,
                StepName = StepNameEnum.Translation,
                StepID = translationStepId,
                StepConfig = JObject.FromObject(new TranslatorConfiguration { 
                    IsInputSegmented= false,
                    Route = "",
                    Endpoint = ""
                }),
                Inputs = new List<string> { "Dataset.STT_RecognitionResult" }
            };

            this.seriesPostBody = new PostBody
            {
                Input = "",
                ExpectedOutput = "",
                ExecutionPipeline = new PipelineInstance(),
                Dataset = new Dictionary<string, string> { {"STT_RecognitionResult", "pathToRecognitionResult.JsonFile" } },
            };
            seriesPostBody.ExecutionPipeline.Name = "TestPipeline";
            seriesPostBody.ExecutionPipeline.PipelineSteps = new List<PipelineStep> { sttStep, translationStep };
            
            Dictionary<string, string> writeToStorageDict = new Dictionary<string, string> { { "pathToRecognitionResult.JsonFile", transcriptionResult } };
            Dictionary<string, string> writeToStorageResultDict = new Dictionary<string, string> { { "pathToRecognitionResult.JsonFile", "AbsoluteFilePath" } };

            var jsonPayload = JsonConvert.SerializeObject(seriesPostBody);
            var durableOrchestrationContextMock = new Mock<IDurableOrchestrationContext>();
            durableOrchestrationContextMock
                .Setup(s => s.GetInput<string>()).Returns(jsonPayload);

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityAsync<string>("GetPipelineInputFromStorage", "pathToRecognitionResult.JsonFile"))
                .ReturnsAsync(transcriptionResult);
            
            durableOrchestrationContextMock
                .Setup(x => x.CallActivityAsync<Dictionary<string, string>>("WritePipelineOutputToStorage", writeToStorageDict))
                .ReturnsAsync(writeToStorageResultDict);

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityAsync<string>(StepNameEnum.SpeechToText.ToString(), It.IsAny<SpeechInput>()))
                .ReturnsAsync(transcriptionResult);
            
            durableOrchestrationContextMock
                .Setup(x => x.CallActivityAsync<string>(StepNameEnum.Translation.ToString(), It.IsAny<TranslatorInput>()))
                .ReturnsAsync(translatedResult);

            //Execute Test
            var result = await durableFunction.RunOrchestrator(durableOrchestrationContextMock.Object);

            Assert.AreEqual(result.Count, 4);
            Assert.AreEqual(result[$"{sttStepId}.{sttResultKey}"], transcriptionResult);
            Assert.AreEqual(result["pathToRecognitionResult.JsonFile"], "AbsoluteFilePath");
            Assert.AreEqual(result[$"{translationStepId}"], translatedResult);

            durableOrchestrationContextMock
                .Verify(mock => mock.CallActivityAsync<string>("GetPipelineInputFromStorage", "pathToRecognitionResult.JsonFile"), Times.Once);

            durableOrchestrationContextMock
                .Verify(mock => mock.CallActivityAsync<Dictionary<string, string>> ("WritePipelineOutputToStorage", writeToStorageDict), Times.Once);
        }

        [TestMethod]
        public async Task RunOrchestrator_Handles_Dataset_AsInputs_For_Parallel_PipelineSteps()
        {
            //Pipeline has one stt and two translations run in parallel. Both translations expect data from blob store

            //setup pipeline and mocks
            string sttStepId = "SpeechToText";
            string lang1TranslationStepId = "HiTranslation";
            string lang2TranslationStepId = "EsTranslation";
            string sttResultKey = "RecognitionResult";
            string transcriptionResult = "This is transcription result";
            string translatedResult = "This is translated result";

            var sttStep = new PipelineStep
            {
                StepOrder = 1,
                StepName = StepNameEnum.SpeechToText,
                StepID = sttStepId,
                StepConfig = JObject.FromObject(new SpeechConfiguration()),
                Outputs = new List<string> { sttResultKey, "Dataset.STT_RecognitionResult" }
            };

            var translateToLang1Step = new PipelineStep
            {
                StepOrder = 3,
                StepName = StepNameEnum.Translation,
                StepID = lang1TranslationStepId,
                StepConfig = JObject.FromObject(new TranslatorConfiguration
                {
                    IsInputSegmented = false,
                    Route = "/translate?api-version=3.0&from=en&to=hi",
                    Endpoint = ""
                }),
                Inputs = new List<string> { "Dataset.STT_RecognitionResult" }
            };

            var translateToLang2Step = new PipelineStep
            {
                StepOrder = 4,
                StepName = StepNameEnum.Translation,
                StepID = lang2TranslationStepId,
                StepConfig = JObject.FromObject(new TranslatorConfiguration
                {
                    IsInputSegmented = false,
                    Route = "/translate?api-version=3.0&from=en&to=es",
                    Endpoint = ""
                }),
                Inputs = new List<string> { "Dataset.STT_RecognitionResult" }
            };

            var parallelPipeLineStep = new PipelineStep
            {
                StepOrder = 2,
                StepName = StepNameEnum.ParallelStep,
                StepID = StepNameEnum.ParallelStep.ToString(),
                ExecutionMode = StepExecutionModeEnum.Parallel,
                PipelineSteps = new List<PipelineStep>() { translateToLang1Step, translateToLang2Step }
            };

            parallelPostBody = new PostBody
            {
                Input = "",
                ExpectedOutput = "",
                ExecutionPipeline = new PipelineInstance(),
                Dataset = new Dictionary<string, string> { { "STT_RecognitionResult", "pathToRecognitionResult.JsonFile" } },
            };
            parallelPostBody.ExecutionPipeline.Name = "TestParallelPipeline";
            parallelPostBody.ExecutionPipeline.PipelineSteps = new List<PipelineStep> { sttStep, parallelPipeLineStep };

            Dictionary<string, string> writeToStorageDict = new Dictionary<string, string> { { "pathToRecognitionResult.JsonFile", transcriptionResult } };
            Dictionary<string, string> writeToStorageResultDict = new Dictionary<string, string> { { "pathToRecognitionResult.JsonFile", "AbsoluteFilePath" } };

            var jsonPayload = JsonConvert.SerializeObject(parallelPostBody);

            var durableOrchestrationContextMock = new Mock<IDurableOrchestrationContext>();

            durableOrchestrationContextMock
                .Setup(s => s.GetInput<string>()).Returns(jsonPayload);

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityAsync<string>("GetPipelineInputFromStorage", "pathToRecognitionResult.JsonFile"))
                .ReturnsAsync(transcriptionResult);

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityAsync<Dictionary<string, string>>("WritePipelineOutputToStorage", writeToStorageDict))
                .ReturnsAsync(writeToStorageResultDict);

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityAsync<string>(StepNameEnum.SpeechToText.ToString(), It.IsAny<SpeechInput>()))
                .ReturnsAsync(transcriptionResult);

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityAsync<string>(StepNameEnum.Translation.ToString(), It.IsAny<TranslatorInput>()))
                .ReturnsAsync(translatedResult);

            //Execute Test
            var result = await durableFunction.RunOrchestrator(durableOrchestrationContextMock.Object);

            Assert.AreEqual(result.Count, 5);
            Assert.AreEqual(result[$"{sttStepId}.{sttResultKey}"], transcriptionResult);
            Assert.AreEqual(result["pathToRecognitionResult.JsonFile"], "AbsoluteFilePath");
            Assert.AreEqual(result[$"{lang1TranslationStepId}"], translatedResult);
            Assert.AreEqual(result[$"{lang2TranslationStepId}"], translatedResult);

            //verify that both translation steps got data from blob storage not previous pipeline output
            durableOrchestrationContextMock
                .Verify(mock => mock.CallActivityAsync<string>("GetPipelineInputFromStorage", "pathToRecognitionResult.JsonFile"), Times.Exactly(2));

            durableOrchestrationContextMock
                .Verify(mock => mock.CallActivityAsync<Dictionary<string, string>>("WritePipelineOutputToStorage", writeToStorageDict), Times.Once);
        }

        [TestMethod]
        public async Task RunOrchestrator_Handles_Different_Inputs_From_Dictionary_for_Parallel_PipelineSteps()
        {
            //Pipeline has one stt and two translations run in parallel.
            //Both translations expect data different outputs produced from previous step - STT

            //setup pipeline and mocks
            string sttStepId = "SpeechToText";
            string lang1TranslationStepId = "Lang1Translation";
            string lang2TranslationStepId = "Lang2Translation";
            string sttResultKey = "RecognitionResult";
            string sttDetailedResultKey = "DetailedRecognitionResult";
            string transcriptionResult = "This is simple transcription result";
            string translatedResult = "This is translated result";

            TranslatorInputSegment segment1 = new TranslatorInputSegment("Detailed Recognition Result 1");
            TranslatorInputSegment segment2 = new TranslatorInputSegment("Detailed Recognition Result 2");
            List<TranslatorInputSegment> detailedTranscriptionResult = new List<TranslatorInputSegment> { segment1, segment2 };

            var sttStep = new PipelineStep
            {
                StepOrder = 1,
                StepName = StepNameEnum.SpeechToText,
                StepID = sttStepId,
                StepConfig = JObject.FromObject(new SpeechConfiguration()),
                Outputs = new List<string> { sttResultKey, sttDetailedResultKey }
            };

            var translateToLang1Step = new PipelineStep
            {
                StepOrder = 3,
                StepName = StepNameEnum.Translation,
                StepID = lang1TranslationStepId,
                StepConfig = JObject.FromObject(new TranslatorConfiguration
                {
                    IsInputSegmented = false,
                    Route = "/translate?api-version=3.0&from=en&to=hi",
                    Endpoint = ""
                }),
                Inputs = new List<string> { $"{sttStepId}.{sttResultKey}" }
            };

            var translateToLang2Step = new PipelineStep
            {
                StepOrder = 4,
                StepName = StepNameEnum.Translation,
                StepID = lang2TranslationStepId,
                StepConfig = JObject.FromObject(new TranslatorConfiguration
                {
                    IsInputSegmented = true,
                    Route = "/translate?api-version=3.0&from=en&to=es",
                    Endpoint = ""
                }),
                Inputs = new List<string> { $"{sttStepId}.{sttDetailedResultKey}" }
            };

            var parallelPipeLineStep = new PipelineStep
            {
                StepOrder = 2,
                StepName = StepNameEnum.ParallelStep,
                StepID = StepNameEnum.ParallelStep.ToString(),
                ExecutionMode = StepExecutionModeEnum.Parallel,
                PipelineSteps = new List<PipelineStep>() { translateToLang1Step, translateToLang2Step }
            };

            parallelPostBody = new PostBody
            {
                Input = "",
                ExpectedOutput = "",
                ExecutionPipeline = new PipelineInstance(),
                Dataset = new Dictionary<string, string> { { "STT_RecognitionResult", "pathToRecognitionResult.JsonFile" } },
            };
            parallelPostBody.ExecutionPipeline.Name = "TestParallelPipeline";
            parallelPostBody.ExecutionPipeline.PipelineSteps = new List<PipelineStep> { sttStep, parallelPipeLineStep };

            var jsonPayload = JsonConvert.SerializeObject(parallelPostBody);

            var durableOrchestrationContextMock = new Mock<IDurableOrchestrationContext>();

            durableOrchestrationContextMock
                .Setup(s => s.GetInput<string>()).Returns(jsonPayload);

            //setup stt module to return two results (a serialized list of strings)
            string sttOutput = JsonConvert.SerializeObject(new List<object>
            {
                transcriptionResult,
                detailedTranscriptionResult
            });

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityAsync<string>(StepNameEnum.SpeechToText.ToString(), It.IsAny<SpeechInput>()))
                .ReturnsAsync(sttOutput);

            //setup translatorInput with right input expecteed from dictionary
            TranslatorConfiguration translatorConfig = durableFunction.UpdateTranslatorConfigFromAppSettings(JsonConvert.SerializeObject(translateToLang1Step.StepConfig));
            var segmentedInput = new List<TranslatorInputSegment>() { new TranslatorInputSegment(transcriptionResult) };
            var translatorInput = new TranslatorInput(translatorConfig, segmentedInput);

            durableOrchestrationContextMock
                .Setup(x => x.CallActivityAsync<string>(StepNameEnum.Translation.ToString(), 
                    It.Is<TranslatorInput>(o => o.Input.Any(s => s.SourceText == transcriptionResult))))
                .ReturnsAsync(translatedResult);

            translatorInput = new TranslatorInput(translatorConfig, detailedTranscriptionResult);
            durableOrchestrationContextMock
                .Setup(x => x.CallActivityAsync<string>(StepNameEnum.Translation.ToString(),
                    It.Is<TranslatorInput>(o => o.Input.Count == detailedTranscriptionResult.Count)))
                .ReturnsAsync(translatedResult);

            //Execute Test
            var result = await durableFunction.RunOrchestrator(durableOrchestrationContextMock.Object);

            Assert.AreEqual(result.Count, 5);
            Assert.AreEqual(result[$"{sttStepId}.{sttResultKey}"], transcriptionResult);
            Assert.AreEqual(result[$"{lang1TranslationStepId}"], translatedResult);
            Assert.AreEqual(result[$"{lang2TranslationStepId}"], translatedResult);
            List<TranslatorInputSegment> segments = JsonConvert.DeserializeObject<List<TranslatorInputSegment>>(result[$"{sttStepId}.{sttDetailedResultKey}"]);
            Assert.AreEqual(segments.Count, detailedTranscriptionResult.Count);

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Execution_Pipeline_Throws_ArgumentNullException_WhenStepConfigIsNull()
        {
            string transcriptionResult = "This is transcription result";

            var singleStep = new PipelineStep
            {
                StepOrder = 1,
                StepID = "Step2",
                StepName = StepNameEnum.SpeechToText
            };

            var singleStepPostBody = new PostBody
            {
                Input = "I want 10 50 dollar tickets for this weekends game",
                ExpectedOutput = "TestOutput_NotUsed",
                ExecutionPipeline = new PipelineInstance()
            };
            singleStepPostBody.ExecutionPipeline.Name = "TestPipeline";
            singleStepPostBody.ExecutionPipeline.PipelineSteps = new List<PipelineStep> { singleStep };

            var jsonPayload = JsonConvert.SerializeObject(singleStepPostBody);

            var durableOrchestrationContextMock = new Mock<IDurableOrchestrationContext>();
            durableOrchestrationContextMock
                .Setup(s => s.GetInput<string>()).Returns(jsonPayload);

            durableOrchestrationContextMock.Setup(x => x.CallActivityAsync<string>(StepNameEnum.SpeechToText.ToString(), It.IsAny<SpeechInput>())).ReturnsAsync(transcriptionResult);

            //Execute Test
            var result = await durableFunction.RunOrchestrator(durableOrchestrationContextMock.Object);
        }

        [TestMethod]
        public async Task SpeechService_Returns_Transcription()
        {
            //Setup
            string transcriptionResult = "This is transcription result";
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var speechServiceMock = new Mock<ISpeechToText>();
            speechServiceMock.Setup(s => s.DoTranscription(It.IsAny<SpeechInput>())).ReturnsAsync(transcriptionResult);

            var postProcessSTTMock = new Mock<IPostProcessSTT>();
            var speechCorrectnessEvaluationMock = new Mock<ISpeechCorrectnessEvaluator>();
            var translatorMock = new Mock<ITranslator>();
            var translatorCorrectnessEvaluationMock = new Mock<ITranslationCorrectnessEvaluator>();
            var ttsPreProcessMock = new Mock<IPreProcessTTS>();
            var ttsServiceMock = new Mock<ITextToSpeech>();
            var storageMgrMock = new Mock<IStorageManager>();
            var subtitlesWritingMock = new Mock<ISubtitlesWriting>();

            durableFunction = new TestingFrameworkOrchestrator(
                loggerMock.Object,
                speechServiceMock.Object,
                postProcessSTTMock.Object,
                speechCorrectnessEvaluationMock.Object,
                translatorMock.Object,
                translatorCorrectnessEvaluationMock.Object,
                ttsPreProcessMock.Object,
                ttsServiceMock.Object,
                subtitlesWritingMock.Object,
                storageMgrMock.Object);

            //Execute Test
            SpeechInput input = new SpeechInput(new SpeechConfiguration(), "Test Input");
            var result = await durableFunction.DoTranscriptionAsync(input);

            //assert output has return values
            Assert.AreEqual(transcriptionResult, result);
        }

        [TestMethod]
        public async Task TTSService_Returns_AudioFile()
        {
            //Setup
            string wavFilePath = "tts_0.wav";
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            
            var speechServiceMock = new Mock<ISpeechToText>();
            var postProcessSTTMock = new Mock<IPostProcessSTT>();
            var speechCorrectnessEvaluationMock = new Mock<ISpeechCorrectnessEvaluator>();
            var translatorMock = new Mock<ITranslator>();
            var translatorCorrectnessEvaluationMock = new Mock<ITranslationCorrectnessEvaluator>();
            var ttsPreProcessMock = new Mock<IPreProcessTTS>();
            
            var ttsServiceMock = new Mock<ITextToSpeech>();
            ttsServiceMock.Setup(s => s.GenerateAudioAsync(It.IsAny<TextToSpeechInput>())).ReturnsAsync(wavFilePath);

            var storageMgrMock = new Mock<IStorageManager>();
            var subtitlesWritingMock = new Mock<ISubtitlesWriting>();


            durableFunction = new TestingFrameworkOrchestrator(
                loggerMock.Object,
                speechServiceMock.Object,
                postProcessSTTMock.Object,
                speechCorrectnessEvaluationMock.Object,
                translatorMock.Object,
                translatorCorrectnessEvaluationMock.Object,
                ttsPreProcessMock.Object,
                ttsServiceMock.Object,
                subtitlesWritingMock.Object,
                storageMgrMock.Object);
            
            //Execute Test
            TTSPreProcessingOutput ttsPreProcessOutput = new TTSPreProcessingOutput();
            ttsPreProcessOutput.TextToSpeak = "Hello how are you?";

            List<TTSPreProcessingOutput> ttsList = new List<TTSPreProcessingOutput> { ttsPreProcessOutput };

            TextToSpeechInput input = new TextToSpeechInput(new TextToSpeechConfiguration(), ttsList[0].Ssml);
            var result = await durableFunction.GenerateSpeechAudio(input);

            //assert output has return values
            Assert.AreEqual(wavFilePath, result);
        }
    }
}
