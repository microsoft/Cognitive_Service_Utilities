using AIPlatform.TestingFramework.Utilities.Diagnostics;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using AIPlatform.TestingFramework.Common;

namespace AIPlatform.TestingFramework.STT.TranscriptionUtils
{
    public class ContinuousLanguageIDTranscriber
    {
        public static async Task<List<SpeechOutputSegment>> RunContinuousLanguageIDAsync(
            SpeechInput inputConfig, IOrchestratorLogger<TestingFrameworkOrchestrator> logger, byte[] fileData)
        {
            List<SpeechOutputSegment> results = new List<SpeechOutputSegment>();

            var speechConfig = inputConfig.StepConfiguration.ServiceConfiguration;

            string speechKey = speechConfig.SubscriptionKey;
            string region = speechConfig.Region;
            List<string> candidateLocales = inputConfig.StepConfiguration.ContinuousLID.CandidateLocales;

            PushAudioInputStream pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));

            var endpointString = $"wss://{region}.stt.speech.microsoft.com/speech/universal/v2";
            var endpointUrl = new Uri(endpointString);
            var config = SpeechConfig.FromEndpoint(endpointUrl, speechKey);
            config.OutputFormat = OutputFormat.Detailed;
            foreach (KeyValuePair<string, string> item in inputConfig.StepConfiguration.ServiceProperty)
            {
                config.SetServiceProperty(item.Key, item.Value, ServicePropertyChannel.UriQueryParameter);
            }

            config.SetProperty(PropertyId.SpeechServiceConnection_ContinuousLanguageIdPriority, "Accuracy");
            config.SetServiceProperty("displayWordTimings", "true", ServicePropertyChannel.UriQueryParameter);
            config.SetServiceProperty("setfeature", "wfstdisplaytimings", ServicePropertyChannel.UriQueryParameter);
            var autoDetectSourceLanguageConfig = AutoDetectSourceLanguageConfig.FromLanguages(candidateLocales.ToArray());
            var audioConfig = AudioConfig.FromStreamInput(pushStream);
            SpeechRecognizer recognizer = new SpeechRecognizer(config, autoDetectSourceLanguageConfig, audioConfig);

            var stopRecognition = new TaskCompletionSource<int>();

            RegisterRecognizerCallbacks(results, stopRecognition, recognizer, logger);

            await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

            WriteDataToPushStream(pushStream, fileData);

            Task.WaitAny(new[] { stopRecognition.Task });

            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

            return results;
        }

        private static void RegisterRecognizerCallbacks(
            List<SpeechOutputSegment> results, TaskCompletionSource<int> stopRecognition, SpeechRecognizer recognizer,
            IOrchestratorLogger<TestingFrameworkOrchestrator> logger)
        {
            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    var detailsJSONtring = e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
                    DetailedTranscriptionOutputResultSegment details = JsonConvert.DeserializeObject<DetailedTranscriptionOutputResultSegment>(detailsJSONtring);
                    NBest selectedResult = details.GetSelectedNBestResult();

                    SpeechOutputSegment speechOutputSegment = new SpeechOutputSegment();
                    logger.LogInformation($"RECOGNIZED language: {details.PrimaryLanguage.Language} text: {details.DisplayText}");

                    speechOutputSegment.DisplayText = details.DisplayText;
                    speechOutputSegment.LexicalText = selectedResult.LexicalText;
                    speechOutputSegment.IdentifiedSpeaker = null;
                    speechOutputSegment.IdentifiedLocale = details.PrimaryLanguage.Language;
                    speechOutputSegment.Duration = details.Duration;
                    speechOutputSegment.Offset = details.Offset;
                    speechOutputSegment.SegmentID = 0;

                    List<TimeStamp> lexicalTimeStamps = new List<TimeStamp>();

                    foreach (TimeStamp word in selectedResult.Words)
                    {
                        TimeStamp timestamp = new TimeStamp(word.Word, word.Duration, word.Offset);
                        lexicalTimeStamps.Add(timestamp);
                    }

                    speechOutputSegment.TimeStamps = lexicalTimeStamps;

                    List<TimeStamp> displayTimeStamps = new List<TimeStamp>();

                    foreach (TimeStamp word in selectedResult.DisplayWords)
                    {
                        TimeStamp timestamp = new TimeStamp(word.Word, word.Duration, word.Offset);
                        displayTimeStamps.Add(timestamp);
                    }

                    speechOutputSegment.DisplayWordTimeStamps = displayTimeStamps;

                    List<SpeechCandidate> nBest = new List<SpeechCandidate>();

                    foreach (NBest transcription in details.NBest)
                    {
                        SpeechCandidate speechCandidate = new SpeechCandidate()
                        {
                            LexicalText = transcription.LexicalText,
                            Confidence = transcription.Confidence,
                            Words = transcription.Words,
                        };

                        nBest.Add(speechCandidate);
                    }

                    speechOutputSegment.NBest = nBest;

                    results.Add(speechOutputSegment);
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    logger.LogInformation($"Nomatch details: {NoMatchDetails.FromResult(e.Result)}");
                    logger.LogInformation($"NOMATCH: Speech could not be recognized.");
                }
            };

            recognizer.Canceled += (s, e) =>
            {
                logger.LogInformation($"CANCELED:\tReason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    logger.LogInformation($"CANCELED: ErrorCode={e.ErrorCode}");
                    logger.LogInformation($"CANCELED: ErrorDetails={e.ErrorDetails}");
                }

                stopRecognition.TrySetResult(0);
            };

            recognizer.SessionStarted += (s, e) =>
            {
                logger.LogInformation($"SESSION STARTED. SessionId: {e.SessionId}.");
            };

            recognizer.SessionStopped += (s, e) =>
            {
                logger.LogInformation($"SESSION STOPPED. SessionId: {e.SessionId}");
                stopRecognition.TrySetResult(0);
            };
        }

        private static void WriteDataToPushStream(PushAudioInputStream pushStream, byte[] fileData)
        {
            pushStream.Write(fileData);
            pushStream.Close();

        }
    }
}
