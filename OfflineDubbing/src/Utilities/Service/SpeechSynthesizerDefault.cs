//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;

namespace AIPlatform.TestingFramework.Utilities.Service
{
    public class SpeechSynthesizerDefault : ISpeechSynthesizer
    {
        SpeechSynthesizer synthesizer;
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;
        public SpeechSynthesizerDefault(string SubscriptionKey, string Region, IOrchestratorLogger<TestingFrameworkOrchestrator> logger)
        {
            var config = SpeechConfig.FromSubscription(SubscriptionKey, Region);
            synthesizer = new SpeechSynthesizer(config, null);
            this.logger = logger;
        }

        public SpeechResult SpeakSsmlAsync(string SSML)
        {
            var result = synthesizer.SpeakSsmlAsync(SSML).Result;

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                return new SpeechResult(result.AudioData, result.AudioDuration);
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                logger.LogError($"CANCELED: Reason={cancellation.Reason}. ResultId: {result.ResultId}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    logger.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}, ErrorDetails=[{cancellation.ErrorDetails}]");
                }
                throw new Exception("Failed to generate Text to Speech");
            }
            return null;
        }
        public SpeechResult SpeakTextAsync(string text)
        {
            var result = synthesizer.SpeakTextAsync(text).Result;

            if (result.Reason == ResultReason.SynthesizingAudioCompleted)
            {
                return new SpeechResult(result.AudioData, result.AudioDuration);
            }
            else if (result.Reason == ResultReason.Canceled)
            {
                var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                logger.LogError($"CANCELED: Reason={cancellation.Reason}. ResultId: {result.ResultId}");

                if (cancellation.Reason == CancellationReason.Error)
                {
                    logger.LogError($"CANCELED: ErrorCode={cancellation.ErrorCode}, ErrorDetails=[{cancellation.ErrorDetails}]");
                }
                throw new Exception("Failed to generate Text to Speech");
            }
            return null;
        }
    }
}
