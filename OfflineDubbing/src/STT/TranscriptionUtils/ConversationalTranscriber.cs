//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Transcription;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.STT.TranscriptionUtils
{
    public class ConversationalTranscriber
    {
        public static async Task<List<SpeechOutputSegment>> RunConversationTranscriptionAsync(
            SpeechInput inputConfig, 
            IOrchestratorLogger<TestingFrameworkOrchestrator> logger, 
            IHttpClientFactory httpClientFactory,
            byte[] fileData)
        {

            List<SpeechOutputSegment> results = new List<SpeechOutputSegment>();

            var speechConfig = inputConfig.StepConfiguration.ServiceConfiguration;

            string speechKey = speechConfig.SubscriptionKey;
            string region = speechConfig.Region;

            //TODO: check if ConversationTranscriber works with custom Endpoint, currently it isnt used.
            string endPoint = inputConfig.StepConfiguration.EndpointId;
            
            List<CTSpeaker> speakers = inputConfig.StepConfiguration.ConversationTranscription.Speakers;

            Dictionary<string, string> signatureNameMap = new Dictionary<string, string>();

            if (speakers != null)
            {
                signatureNameMap = await RegisterVoices(speakers, speechKey, region, httpClientFactory);
            }

            var config = SpeechConfig.FromSubscription(speechKey, region);
            config.SetProperty("ConversationTranscriptionInRoomAndOnline", "true");
            config.SetProperty("DifferentiateGuestSpeakers", "true");
            config.SetProperty("TranscriptionService_SingleChannel", "true");
            config.SetServiceProperty("displayWordTimings", "true", ServicePropertyChannel.UriQueryParameter);
            config.SetServiceProperty("setfeature", "wfstdisplaytimings", ServicePropertyChannel.UriQueryParameter);
            config.OutputFormat = OutputFormat.Detailed;
            config.SpeechRecognitionLanguage = inputConfig.StepConfiguration.Locale;

            PushAudioInputStream pushStream = AudioInputStream.CreatePushStream(AudioStreamFormat.GetWaveFormatPCM(16000, 16, 1));

            var audioConfig = AudioConfig.FromStreamInput(pushStream);
            var meetingID = Guid.NewGuid().ToString();
            var conversation = await Conversation.CreateConversationAsync(config, meetingID);
            var conversationTranscriber = new ConversationTranscriber(audioConfig);

            var stopRecognition = new TaskCompletionSource<int>();

            RegisterRecognizerCallbacks(inputConfig, logger, results, conversationTranscriber, stopRecognition);

            foreach (string name in signatureNameMap.Keys)
            {
                await conversation.AddParticipantAsync(Participant.From(name, "en-US", signatureNameMap[name]));
            }

            await conversationTranscriber.JoinConversationAsync(conversation);

            var connection = Connection.FromRecognizer(conversationTranscriber);
            connection.SetMessageProperty("speech.config", "DisableReferenceChannel", $"\"True\"");
            connection.SetMessageProperty("speech.config", "MicSpec", $"\"1_0_0\"");

            await conversationTranscriber.StartTranscribingAsync().ConfigureAwait(false);

            WriteDataToPushStream(pushStream, fileData);
            
            Task.WaitAny(new[] { stopRecognition.Task });
            
            await conversationTranscriber.StopTranscribingAsync().ConfigureAwait(false);

            return results;
        }

        private static void RegisterRecognizerCallbacks(SpeechInput inputConfig,
            IOrchestratorLogger<TestingFrameworkOrchestrator> logger,
            List<SpeechOutputSegment> results,
            ConversationTranscriber conversationTranscriber,
            TaskCompletionSource<int> stopRecognition)
        {
            conversationTranscriber.Transcribed += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    if (e.Result.Duration.Ticks != 0 && !string.IsNullOrEmpty(e.Result.Text))
                    {
                        logger.LogInformation($"TRANSCRIBED details: User: {e.Result.UserId}\tUtteranceId: {e.Result.UtteranceId}\tOffset(seconds): {TimeSpan.FromTicks(e.Result.OffsetInTicks).TotalSeconds}\nText: {e.Result.Text}");

                        var detailsJSONstring = e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult);
                        DetailedTranscriptionOutputResultSegment details = JsonConvert.DeserializeObject<DetailedTranscriptionOutputResultSegment>(detailsJSONstring);
                        NBest selectedResult = details.GetSelectedNBestResult();
                        SpeechOutputSegment speechOutputSegment = new SpeechOutputSegment();

                        speechOutputSegment.DisplayText = details.DisplayText;
                        speechOutputSegment.LexicalText = selectedResult.LexicalText;
                        speechOutputSegment.IdentifiedSpeaker = e.Result.UserId;
                        speechOutputSegment.IdentifiedLocale = inputConfig.StepConfiguration.Locale;
                        speechOutputSegment.Duration = details.Duration;
                        speechOutputSegment.Offset = details.Offset;
                        speechOutputSegment.SegmentID = 0;

                        List<string> lexicalStringSegments = new List<string>();
                        List<TimeStamp> lexicalTimeStamps = new List<TimeStamp>();

                        foreach (TimeStamp word in selectedResult.Words)
                        {
                            TimeStamp timestamp = new TimeStamp(word.Word, word.Duration, word.Offset);
                            lexicalStringSegments.Add(word.Word);
                            lexicalTimeStamps.Add(timestamp);
                        }

                        speechOutputSegment.LexicalText = String.Join(" ", lexicalStringSegments);
                        speechOutputSegment.TimeStamps = lexicalTimeStamps;

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
                }
                else if (e.Result.Reason == ResultReason.NoMatch)
                {
                    logger.LogInformation($"NOMATCH: Speech could not be recognized.");
                }
            };

            conversationTranscriber.Canceled += (s, e) =>
            {
                logger.LogInformation($"CANCELED: Reason={e.Reason}");

                if (e.Reason == CancellationReason.Error)
                {
                    logger.LogInformation($"CANCELED: ErrorCode={e.ErrorCode}. ErrorDetails={e.ErrorDetails}");
                }
                stopRecognition.TrySetResult(0);
            };

            conversationTranscriber.SessionStarted += (s, e) =>
            {
                logger.LogInformation($"\nSESSION STARTED event. SessionId={e.SessionId}");
            };

            conversationTranscriber.SessionStopped += (s, e) =>
            {
                logger.LogInformation($"\nSESSION STOPPED SessionId={e.SessionId}");
                stopRecognition.TrySetResult(0);
            };
        }

        private static void WriteDataToPushStream(PushAudioInputStream pushStream, byte[] fileData)
        {
            pushStream.Write(fileData);
            pushStream.Close();

        }

        private static async Task<Dictionary<string, string>> RegisterVoices(List<CTSpeaker> speakers, string speechKey, string region, IHttpClientFactory httpClientFactory)
        {
            Dictionary<string, string> signatureNameMap = new Dictionary<string, string>();

            foreach (CTSpeaker speaker in speakers)
            {
                string signature = await GetVoiceSignatureString(speechKey, region, speaker.VoiceSample, httpClientFactory);
                signatureNameMap[speaker.Name] = signature;
            }

            return signatureNameMap;
        }

        private static async Task<string> GetVoiceSignatureString(string subscriptionKey, string region, string voice_sample_path, IHttpClientFactory httpClientFactory)
        {
            HttpClient voiceFileHTTPClient = httpClientFactory.CreateClient();
            byte[] fileBytes = await voiceFileHTTPClient.GetByteArrayAsync(voice_sample_path);

            var content = new ByteArrayContent(fileBytes);
            var voiceRegisterHTTPClient = httpClientFactory.CreateClient();
            voiceRegisterHTTPClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            var response = await voiceRegisterHTTPClient.PostAsync($"https://signature.{region}.cts.speech.microsoft.com/api/v1/Signature/GenerateVoiceSignatureFromByteArray", content);

            var jsonData = await response.Content.ReadAsStringAsync();
            var result = JsonConvert.DeserializeObject<VoiceSignature>(jsonData);
            return JsonConvert.SerializeObject(result.Signature);
        }
    }
}
