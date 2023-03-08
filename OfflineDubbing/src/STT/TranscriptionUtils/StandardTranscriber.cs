using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.STT.TranscriptionUtils
{
    public class StandardTranscriber
    {
        public static async Task<List<SpeechOutputSegment>> RunStandardTranscriptionAsync(
            SpeechInput inputConfig, 
            IOrchestratorLogger<TestingFrameworkOrchestrator> logger, byte[] fileData)
        {
            List<SpeechOutputSegment> results = new List<SpeechOutputSegment>();

            string locale = inputConfig.StepConfiguration.Locale;

            if (string.IsNullOrEmpty(locale))
            {
                locale = "en-US";
            }

            var speechConfig = inputConfig.StepConfiguration.ServiceConfiguration;

            string speechKey = speechConfig.SubscriptionKey;
            string region = speechConfig.Region;
            string endPoint = inputConfig.StepConfiguration.EndpointId;

            PushAudioInputStream pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));

            SpeechConfig config = SpeechConfig.FromSubscription(subscriptionKey: speechKey, region: region);
            config.OutputFormat = OutputFormat.Detailed;
            config.SetServiceProperty("displayWordTimings", "true", ServicePropertyChannel.UriQueryParameter);
            config.SetServiceProperty("setfeature", "wfstdisplaytimings", ServicePropertyChannel.UriQueryParameter);
            foreach (KeyValuePair<string, string> item in inputConfig.StepConfiguration.ServiceProperty)
            {
                config.SetServiceProperty(item.Key, item.Value, ServicePropertyChannel.UriQueryParameter);
            }

            SpeechRecognizer recognizer = CreateSpeechRecognizer(locale, endPoint, config, pushStream);

            var stopRecognition = new TaskCompletionSource<int>();

            RegisterRecognizerCallbacks(results, stopRecognition, locale, recognizer, logger);

            await recognizer.StartContinuousRecognitionAsync();

            WriteDataToPushStream(pushStream, fileData);

            Task.WaitAny(new[] { stopRecognition.Task });

            await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

            return results;
        }

        private static SpeechRecognizer CreateSpeechRecognizer(string locale, string endPoint, SpeechConfig config, PushAudioInputStream pushStream)
        {
            SpeechRecognizer recognizer;
            var audioInput = AudioConfig.FromStreamInput(pushStream);
            if (!string.IsNullOrEmpty(endPoint))
            {
                SourceLanguageConfig sourceLanguageConfig = SourceLanguageConfig.FromLanguage(locale, endPoint);
                recognizer = new SpeechRecognizer(config, sourceLanguageConfig, audioInput);
            }
            else
            {
                recognizer = new SpeechRecognizer(config, locale, audioInput);
            }

            return recognizer;
        }

        private static void RegisterRecognizerCallbacks(List<SpeechOutputSegment> results,
            TaskCompletionSource<int> stopRecognition, string locale, SpeechRecognizer recognizer, IOrchestratorLogger<TestingFrameworkOrchestrator> logger)
        {
            recognizer.Recognized += (s, e) =>
            {
                if ( (e.Result.Reason == ResultReason.RecognizedSpeech) && (!string.IsNullOrEmpty(e.Result.Text)) )
                {
                    logger.LogInformation($"RECOGNIZED details: {e.Result.Text}");
                    var detailsJSONtring = e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
                    DetailedTranscriptionOutputResultSegment details = JsonConvert.DeserializeObject<DetailedTranscriptionOutputResultSegment>(detailsJSONtring);
                    SpeechOutputSegment speechOutputSegment = new SpeechOutputSegment();

                    speechOutputSegment.DisplayText = details.DisplayText;
                    NBest selectedResult = details.GetSelectedNBestResult();
                    speechOutputSegment.LexicalText = selectedResult.LexicalText;
                    speechOutputSegment.IdentifiedSpeaker = null;
                    speechOutputSegment.IdentifiedLocale = locale;
                    speechOutputSegment.Duration = details.Duration;
                    speechOutputSegment.Offset = details.Offset;
                    speechOutputSegment.SegmentID = 0;

                    List<TimeStamp> lexicalTimeStamps = new List<TimeStamp>();

                    foreach(TimeStamp word in selectedResult.Words)
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
                            Words = transcription.Words
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
