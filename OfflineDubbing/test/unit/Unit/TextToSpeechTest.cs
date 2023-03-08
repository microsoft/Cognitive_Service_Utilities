using AIPlatform.TestingFramework;
using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.TTS;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Service;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using NAudio.Wave;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace AIPlatform.EvaluationFramework.Test.Unit
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class TextToSpeechTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task TextToSpeechService_ThrowsArgumentNullException_If_Region_Is_NullOrEmpty()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var speechSynthesizerMock = new TestUnitSpeechSynthesizer();

            //Execute Test
            var TTSService = new TextToSpeech(loggerMock.Object, storageManagerMock.Object, speechSynthesizerMock);

            CognitiveServiceConfiguration svcConfig = new CognitiveServiceConfiguration
            {
                Region = string.Empty
            };

            TextToSpeechConfiguration config = new TextToSpeechConfiguration
            {
                ServiceConfiguration = svcConfig
            };

            await TTSService.GenerateAudioAsync(new TextToSpeechInput(config, ""));

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task TextToSpeechService_ThrowsArgumentNullException_If_Subscription_Is_NullOrEmpty()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var speechSynthesizerMock = new TestUnitSpeechSynthesizer();

            //Execute Test
            var TTSService = new TextToSpeech(loggerMock.Object, storageManagerMock.Object, speechSynthesizerMock);

            CognitiveServiceConfiguration svcConfig = new CognitiveServiceConfiguration
            {
                SubscriptionKey = string.Empty
            };

            TextToSpeechConfiguration config = new TextToSpeechConfiguration
            {
                ServiceConfiguration = svcConfig
            };

            await TTSService.GenerateAudioAsync(new TextToSpeechInput(config, ""));

        }

        [TestMethod]
        public void TextToSpeechService_ConcatsSilenceWithBreaks()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new MockBinaryFileStorageManager();
            var speechSynthesizerMock = new TestUnitSpeechSynthesizer();

            var inputSSML = @"<?xml version=""1.0""?>
                                    <speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xmlns:mstts=""https://www.w3.org/2001/mstts"" xml:lang=""en-US"">
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""0ms""/>
                                        <prosody rate=""1"">यह खंड 1 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""250ms""/>
                                        <prosody rate=""1"">यह खंड 2 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""250ms""/>
                                        <prosody rate=""1"">यह खंड 3 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""250ms""/>
                                        <prosody rate=""1"">यह खंड 4 है</prosody>
                                      </voice>
                                    </speak>";

            CognitiveServiceConfiguration svcConfig = new CognitiveServiceConfiguration
            {
                SubscriptionKey = "<MOCK-SUBSCRIPTION>",
                Region = "<MOCK-REGION>",
            };

            BlobStorageConfiguration storageConfiguration = new BlobStorageConfiguration
            {
                FolderPath = "<MOCK-FOLDER-PATH>"
            };

            TextToSpeechConfiguration config = new TextToSpeechConfiguration
            {
                ServiceConfiguration = svcConfig,
                StorageConfiguration = storageConfiguration
            };

            var input = new TextToSpeechInput(config, inputSSML);

            //Execute Test
            var speechService = new TextToSpeech(loggerMock.Object, storageManagerMock, speechSynthesizerMock);
            var filepaths = JsonConvert.DeserializeObject<List<string>>(speechService.GenerateAudioAsync(input).Result);

            var expectedDuration = new TimeSpan(0, 0, 0, 0, 4750);

            var actualFile = storageManagerMock.ReadBinaryFileAsync(filepaths[0]).Result;

            MemoryStream actualFileStream = new MemoryStream(actualFile);
            WaveFileReader reader = new WaveFileReader(actualFileStream);
            TimeSpan actualDuration = reader.TotalTime;

            Assert.AreEqual(expectedDuration, actualDuration);
        }

        public class TestUnitSpeechSynthesizer : ISpeechSynthesizer
        {
            readonly byte[] silence;
            public TestUnitSpeechSynthesizer() {
                MemoryStream memoryStream = new MemoryStream();

                SilenceProvider silenceProvider = new SilenceProvider(new WaveFormat(16000, 1));
                var silence = silenceProvider.ToSampleProvider().Take(new TimeSpan(0, 0, 0, 1));

                WaveFileWriter.WriteWavFileToStream(memoryStream, silence.ToWaveProvider());
                this.silence = memoryStream.ToArray();
            }

            public SpeechResult SpeakTextAsync(string text) => new SpeechResult(this.silence, new TimeSpan(0, 0, 1));

            public SpeechResult SpeakSsmlAsync(string SSML) => new SpeechResult(this.silence, new TimeSpan(0, 0, 1));
        }

        public class MockBinaryFileStorageManager : IStorageManager
        {
            readonly Dictionary<string, byte[]> tempStorage;

            public MockBinaryFileStorageManager()
            {
                tempStorage = new Dictionary<string, byte[]>();
            }

            public Task<byte[]> ReadBinaryFileAsync(string filePath) => Task.Run(() => tempStorage[filePath]);

            public Task<List<byte[]>> ReadBinaryFilesAsync(List<string> filePaths) => throw new NotImplementedException();
            public Task<string> ReadTextFileAsync(string filePath) => throw new NotImplementedException();
            public Task<List<string>> WriteFilesToStorageAsync(BlobStorageInput blobStorageInput)
            {
                List<string> filePaths = new List<string>();
                foreach (byte[] binaryFile in blobStorageInput.BinaryFiles)
                {
                    var guid = Guid.NewGuid().ToString();
                    filePaths.Add(guid);
                    tempStorage.Add(guid, binaryFile);
                }

                return Task.Run(() => filePaths);
            }
            public Task<string> WriteTextFileAsync(string fileContent, string filePath, bool overWrite = true) => throw new NotImplementedException();
        }
    }
}
