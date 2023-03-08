using AIPlatform.TestingFramework.Utilities.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AIPlatform.TestingFramework.ExecutionPipeline.Execution;
using AIPlatform.TestingFramework.Utilities.Storage;
using Newtonsoft.Json;
using System.Xml;
using NAudio.Wave;
using System.IO;
using System.Text.RegularExpressions;
using NAudio.Wave.SampleProviders;
using AIPlatform.TestingFramework.Utilities.Service;

namespace AIPlatform.TestingFramework.TTS
{
    public class TextToSpeech: ExecutePipelineStep, ITextToSpeech
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;
        private ISpeechSynthesizer speechSynthesizer;

        public TextToSpeech(IOrchestratorLogger<TestingFrameworkOrchestrator> logger, IStorageManager storageManager, ISpeechSynthesizer speechSynthesizer) :
    base(logger, storageManager)
        {

            this.logger = logger;
            this.speechSynthesizer = speechSynthesizer;
        }

        public async Task<string> GenerateAudioAsync(TextToSpeechInput input)
        {
            if (speechSynthesizer == null)
            {
                speechSynthesizer = new SpeechSynthesizerDefault(input.StepConfiguration.ServiceConfiguration.SubscriptionKey, input.StepConfiguration.ServiceConfiguration.Region, this.logger);
            }

            ValidateStepInput(input);

            List<byte[]> fileDataList = new List<byte[]>();

            List<byte[]> audioFiles = GenerateAudioFiles(input.TTSInput, input.StepConfiguration);

            byte[] concantenatedFile = ConcatAudioFiles(audioFiles);

            logger.LogInformation($"Number of audio files generated (including pause silence: {audioFiles.Count}");
            

            fileDataList.Add(concantenatedFile);

            BlobStorageInput blobWriterInput = new BlobStorageInput(input.StepConfiguration.StorageConfiguration, fileDataList);
            List<string> filePaths = await WriteToBlobStoreAsync(blobWriterInput);

            return JsonConvert.SerializeObject(filePaths);
        }

        public byte[] ConcatAudioFiles(List<byte[]> audioFiles)
        {
            List<ISampleProvider> audioProviders = new List<ISampleProvider>();

            foreach (var audioFile in audioFiles)
            {
                audioProviders.Add(new WaveFileReader(new MemoryStream(audioFile)).ToSampleProvider());
            }
            var concatSampleProvider = new ConcatenatingSampleProvider(audioProviders);

            MemoryStream memoryStream = new MemoryStream();
            WaveFileWriter.WriteWavFileToStream(memoryStream, concatSampleProvider.ToWaveProvider());
            return memoryStream.ToArray();
        }

        public List<byte[]> GenerateAudioFiles(string ssml, TextToSpeechConfiguration ttsConfig)
        {
            List<byte[]> AudioFiles = new List<byte[]>();

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(ssml);
            XmlElement root = doc.DocumentElement;

            logger.LogInformation($"Number of audio segments in SSML: {root?.ChildNodes.Count}");
            for (int counter = 0; counter < root.ChildNodes.Count; counter++)
            {
                XmlNode node = root.ChildNodes[counter];
                string childSSML = $"<speak version = \"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"en-US\">";
                childSSML += $"<voice name = \"{node.Attributes?["name"].Value}\" >";

                childSSML += $"<mstts:silence  type=\"Tailing\" value=\"0ms\"/>";
                childSSML += $"<mstts:silence  type=\"Leading\" value=\"0ms\"/>";

                var rate = node["prosody"]?.Attributes?["rate"]?.Value ?? "1";
                childSSML += $"<prosody rate = \"{rate}\">";
                childSSML += node.InnerText;
                childSSML += "</prosody>";
                childSSML += "</voice>";
                childSSML += "</speak>";

                int breakDuration = Int32.Parse(Regex.Match(node["break"].Attributes?["time"].Value, @"\d+").Value);

                if (breakDuration > 0)
                    AudioFiles.Add(GenerateSilence(breakDuration));

                var speechResult = speechSynthesizer.SpeakSsmlAsync(childSSML);

                AudioFiles.Add(speechResult.AudioData);

                logger.LogInformation($"Segment {counter} Info - Synthesized Audio Duration: {speechResult.AudioDuration} Voice Name: {node.Attributes?["name"].Value} Rate: {rate}, Break Duration (ms): {breakDuration}");
            }

            return AudioFiles;
        }

        public byte[] GenerateSilence(int duration)
        {
            MemoryStream memoryStream = new MemoryStream();

            SilenceProvider silenceProvider = new SilenceProvider(new WaveFormat(16000, 1));
            var silence = silenceProvider.ToSampleProvider().Take(new TimeSpan(0, 0, 0, 0, duration));

            WaveFileWriter.WriteWavFileToStream(memoryStream, silence.ToWaveProvider());
            return memoryStream.ToArray();
        }

        private void ValidateStepInput(TextToSpeechInput input)
        {
            if (input == null)
            {
                logger.LogError($"Argument {nameof(input)} is null");
                throw new ArgumentNullException(nameof(input));
            }

            if (string.IsNullOrEmpty(input.StepConfiguration.ServiceConfiguration.Region))
            {
                logger.LogError($"Argument {nameof(input.StepConfiguration.ServiceConfiguration.Region)} is null or Empty");
                throw new ArgumentNullException(nameof(input.StepConfiguration.ServiceConfiguration.Region));
            }

            if (string.IsNullOrEmpty(input.StepConfiguration.ServiceConfiguration.SubscriptionKey))
            {
                logger.LogError($"Argument {nameof(input.StepConfiguration.ServiceConfiguration.SubscriptionKey)} is null or Empty");
                throw new ArgumentNullException(nameof(input.StepConfiguration.ServiceConfiguration.SubscriptionKey));
            }

            if (input.TTSInput == null)
            {
                logger.LogError($"Argument {nameof(input.TTSInput)} is null");
                throw new ArgumentNullException(nameof(input.TTSInput));
            }
        }
    }
}
