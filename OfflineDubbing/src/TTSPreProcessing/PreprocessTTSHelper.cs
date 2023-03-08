//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using MathNet.Numerics.Statistics;
using AIPlatform.TestingFramework.Utilities.Service;

namespace AIPlatform.TestingFramework.TTSPreProcessing
{
    internal static class PreprocessTTSHelper
    {
        internal static double GetScaledRateAsync(PreProcessTTSInput input, ISpeechSynthesizer synthesizer, IOrchestratorLogger<TestingFrameworkOrchestrator> logger)
        {
            logger.LogInformation($"Calculating speaking rate for:\n{input.TranslatedText}");

            var result = synthesizer.SpeakTextAsync(input.TranslatedText);
            if (result != null)
            {
                TimeSpan audioDuration = result.AudioDuration;
                double ratioAdustPercentage = audioDuration.TotalMilliseconds / input.Duration.TotalMilliseconds;
                logger.LogInformation($"TTS audio duration [MS]: {audioDuration.TotalMilliseconds}, Input segment duration [MS]: {input.Duration.TotalMilliseconds}, speak ratio: {ratioAdustPercentage}% for Segment {input.SegmentID}");

                return ratioAdustPercentage;
            }
            return 1.0f;
        }

        internal static TimeSpan GetTargetDuration(PreProcessTTSInput speechSegment, ISpeechSynthesizer synthesizer)
        {
            string targetSSML = $"<speak version = \"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"en-US\">";
            targetSSML += $"<voice name = \"{speechSegment.VoiceInfo.VoiceName}\" >";
            targetSSML += $"<mstts:silence  type=\"Tailing\" value=\"0ms\"/>";
            targetSSML += $"<mstts:silence  type=\"Leading\" value=\"0ms\"/>";
            targetSSML += $"<prosody rate = \"{speechSegment.Rate}\">";
            targetSSML += speechSegment.TranslatedText;
            targetSSML += "</prosody>";
            targetSSML += "</voice>";
            targetSSML += "</speak>";

            var result = synthesizer.SpeakSsmlAsync(targetSSML);
            return result.AudioDuration;
        }

        internal static double GetRelativeTargetRate(PreProcessTTSInput input, IOrchestratorLogger<TestingFrameworkOrchestrator> logger)
        {

            string sourceLanguage = input.IdentifiedLocale.Split('-')[0];
            string targetLanguage = input.TargetLocale.Split('-')[0];

            double maxTargetRate = input.PreProcessingStepConfig.MaxSpeechRate;
            double minTargetRate = input.PreProcessingStepConfig.MinSpeechRate;

            if (SpeechRateLookup.Rate.ContainsKey(sourceLanguage) && SpeechRateLookup.Rate.ContainsKey(targetLanguage))
            {
                try
                {
                    // lookup values from static table
                    double sourceWordRate = SpeechRateLookup.Rate[sourceLanguage].WordRate;
                    double targetWordRate = SpeechRateLookup.Rate[targetLanguage].WordRate;

                    double sourceCharRate = SpeechRateLookup.Rate[sourceLanguage].CharRate;
                    double targetCharRate = SpeechRateLookup.Rate[targetLanguage].CharRate;


                    int sourceWordCount = input.LexicalText.Split(" ").Length;
                    int sourceCharCount = input.LexicalText.Length;

                    // calculate rate of source segment relative to nominal language rate
                    double sourceRelativeWordRate = ((sourceWordCount / input.Duration.TotalMinutes) / sourceWordRate);
                    double sourceRelativeCharRate = ((sourceCharCount / input.Duration.TotalMinutes) / sourceCharRate);

                    double sourceAverageRelativeRate = (sourceRelativeWordRate + sourceRelativeCharRate) / 2.0;

                    // calculate ratio between the target and source nominal rates
                    double averagedLanguageRateRatio = (((targetWordRate / sourceWordRate) + (targetCharRate / sourceCharRate)) / 2.0);

                    // scale target rate based on source/target ratio AND relative rate of input with respect to nominal source rate
                    double relativeTargetRate = sourceAverageRelativeRate / averagedLanguageRateRatio;

                    relativeTargetRate = Math.Min(relativeTargetRate, maxTargetRate);
                    relativeTargetRate = Math.Max(relativeTargetRate, minTargetRate);

                    return relativeTargetRate;
                }
                catch (DivideByZeroException exception)
                {
                    logger.LogError(exception.Message);
                    return 1.0;
                }
            }
            else
            {
                return 1.0;
            }
        }

        internal static TimeSpan CalculateMedianPause(List<PreProcessTTSInput> inputs)
        {

            List<double> pauses = new List<double>();

            double previousEndingOffset = 0;

            foreach (var input in inputs)
            {
                pauses.Add(input.Offset.TotalMilliseconds - previousEndingOffset);
                previousEndingOffset = input.Offset.TotalMilliseconds + input.Duration.TotalMilliseconds;
            }

            double medianPause = pauses.ToArray().Median();

            return new TimeSpan(0, 0, 0, 0, (int) medianPause);
        }
    }
}
