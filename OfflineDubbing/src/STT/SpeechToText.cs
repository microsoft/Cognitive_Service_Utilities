//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.ExecutionPipeline.Execution;
using AIPlatform.TestingFramework.STT.TranscriptionUtils;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.STT
{
    public class SpeechToText : ExecutePipelineStep, ISpeechToText
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;
        private readonly IHttpClientFactory httpClientFactory;

        public SpeechToText(IOrchestratorLogger<TestingFrameworkOrchestrator> logger,
            IHttpClientFactory httpClientFactory,
            IStorageManager storageManager) : base(logger, storageManager)

        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
        }

        public async Task<string> DoTranscription(SpeechInput input)
        {
            logger.LogInformation("Starting Transcription.");

            ValiateInput(input);

            byte[] fileData = await ReadWavFileForTranscriptionAsync(input);

            // check if additional outputs are required
            bool runContinuousLanguageID = false;
            bool runConversationalTranscription = false;

            if ((input.StepConfiguration.ContinuousLID != null) && (input.StepConfiguration.ContinuousLID.Enabled))
            {
                List<string> candidateLocalesList = input.StepConfiguration.ContinuousLID.CandidateLocales;
                if (candidateLocalesList?.Any() != true)
                {
                    this.logger.LogError($"Argument {nameof(input.StepConfiguration.ContinuousLID.CandidateLocales)} is null or Empty");
                    throw new ArgumentNullException(nameof(input.StepConfiguration.ContinuousLID.CandidateLocales));
                }

                runContinuousLanguageID = true;
            }

            if ((input.StepConfiguration.ConversationTranscription != null) &&
                (input.StepConfiguration.ConversationTranscription.Enabled))
            {
                List<CTSpeaker> speakersList = input.StepConfiguration.ConversationTranscription.Speakers;
                runConversationalTranscription = true;
            }

            // run transcription
            if (runContinuousLanguageID && runConversationalTranscription)
            {
                List<SpeechOutputSegment> conversationalTranscriptionResult = 
                    await ConversationalTranscriber.RunConversationTranscriptionAsync(input, logger, httpClientFactory, fileData);

                List<SpeechOutputSegment> continuousLanguageIDTranscritionResult = 
                    await ContinuousLanguageIDTranscriber.RunContinuousLanguageIDAsync(input, logger, fileData);

                var transcriptionResults = SegmentByUserID(continuousLanguageIDTranscritionResult, conversationalTranscriptionResult);

                return FormatResultOutput(transcriptionResults, input);
            }

            if (runConversationalTranscription)
            {
                List<SpeechOutputSegment> conversationalTranscritionResult = 
                    await ConversationalTranscriber.RunConversationTranscriptionAsync(input, logger, httpClientFactory, fileData);

                return FormatResultOutput(conversationalTranscritionResult, input);
            }

            if (runContinuousLanguageID)
            {
                List<SpeechOutputSegment> continuousLanguageIDTranscritionResult = 
                    await ContinuousLanguageIDTranscriber.RunContinuousLanguageIDAsync(input, logger, fileData);

                return FormatResultOutput(continuousLanguageIDTranscritionResult, input);
            }

            List<SpeechOutputSegment> standardTranscriptionResults = 
                await StandardTranscriber.RunStandardTranscriptionAsync(input, logger, fileData);

            return FormatResultOutput(standardTranscriptionResults, input);
        }

        private void ValiateInput(SpeechInput input)
        {
            if (input == null)
            {
                this.logger.LogError($"Argument {nameof(input)} is null");
                throw new ArgumentNullException(nameof(input));
            }

            if (string.IsNullOrEmpty(input.StepConfiguration.ServiceConfiguration.Region))
            {
                this.logger.LogError($"Argument {nameof(input.StepConfiguration.ServiceConfiguration.Region)} is null or Empty");
                throw new ArgumentNullException(nameof(input.StepConfiguration.ServiceConfiguration.Region));
            }

            if (string.IsNullOrEmpty(input.StepConfiguration.ServiceConfiguration.SubscriptionKey))
            {
                this.logger.LogError($"Argument {nameof(input.StepConfiguration.ServiceConfiguration.SubscriptionKey)} is null or Empty");
                throw new ArgumentNullException(nameof(input.StepConfiguration.ServiceConfiguration.SubscriptionKey));
            }
        }

        private List<SpeechOutputSegment> SegmentByUserID(List<SpeechOutputSegment> transcriptionResult, List<SpeechOutputSegment> conversationalTranscriptionResult)
        {
            List<SpeechOutputSegment> speechOutputSegments = new List<SpeechOutputSegment>();

            SpeechOutputSegment lastSegment = new SpeechOutputSegment
            {
                Offset = new TimeSpan(long.MaxValue),
                Duration = new TimeSpan(long.MaxValue),
                IdentifiedSpeaker = conversationalTranscriptionResult.Last().IdentifiedSpeaker
            };

            conversationalTranscriptionResult.Add(lastSegment);

            transcriptionResult.Reverse();
            Stack<SpeechOutputSegment> speechOutputSegmentsStack = new Stack<SpeechOutputSegment>(transcriptionResult);

            int currUserIndex = 1;

            while (speechOutputSegmentsStack.Count > 0)
            {

                SpeechOutputSegment speechOutputSegment = speechOutputSegmentsStack.Pop();

                while (speechOutputSegment.Offset.Ticks > conversationalTranscriptionResult[currUserIndex].Offset.Ticks)
                {
                    currUserIndex++;
                }

                if (speechOutputSegment.Offset.Ticks + speechOutputSegment.Duration.Ticks < conversationalTranscriptionResult[currUserIndex].Offset.Ticks)
                {
                    speechOutputSegment.IdentifiedSpeaker = conversationalTranscriptionResult[currUserIndex - 1].IdentifiedSpeaker;
                    speechOutputSegments.Add(speechOutputSegment);
                }
                else
                {
                    (SpeechOutputSegment firstSegment, SpeechOutputSegment secondSegment) = speechOutputSegment.SplitSegment(conversationalTranscriptionResult[currUserIndex].Offset);
                    if (!string.IsNullOrEmpty(firstSegment.DisplayText))
                    {
                        speechOutputSegment.IdentifiedSpeaker = conversationalTranscriptionResult[currUserIndex - 1].IdentifiedSpeaker;
                        speechOutputSegments.Add(firstSegment);
                    }
                    if (secondSegment != null)
                    {
                        speechOutputSegmentsStack.Push(secondSegment);
                    }
                    currUserIndex++;
                }
            }

            return speechOutputSegments;
        }

        private string FormatResultOutput(List<SpeechOutputSegment> transcriptionResult, SpeechInput input)
        {
            for (int i = 0; i < transcriptionResult.Count; i++)
            {
                transcriptionResult[i].SegmentID = i;
            }

            if (input.StepConfiguration.IsDetailedOutputFormat == false)
            {
                List<string> transcriptionResultStrings = new List<string>();
                foreach (SpeechOutputSegment speechOutputSegment in transcriptionResult)
                {
                    transcriptionResultStrings.Add(speechOutputSegment.DisplayText);
                }

                //TODO: should this be serialized as well?
                return string.Join(" ", transcriptionResultStrings);
            }
            else
            {
                return JsonConvert.SerializeObject(transcriptionResult);
            }
        }

        private async Task<byte[]> ReadWavFileForTranscriptionAsync(SpeechInput input)
        {
            List<string> filePaths = input.StepConfiguration.StorageConfiguration.FileNames;
            List<byte[]> binaryFiles = await ReadBinaryFilesAsync(filePaths);

            //return just the first file 
            return binaryFiles[0];
        }
    }
}
