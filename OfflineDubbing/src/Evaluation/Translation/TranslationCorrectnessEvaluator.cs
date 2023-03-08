//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Evaluation.Interfaces;
using AIPlatform.TestingFramework.ExecutionPipeline.Execution;
using AIPlatform.TestingFramework.Translation;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.Evaluation.Translation
{
    public class TranslationCorrectnessEvaluator : ExecutePipelineStep, ITranslationCorrectnessEvaluator
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;
        private readonly ITranslator translator;

        public TranslationCorrectnessEvaluator(ITranslator translator, IStorageManager storageManager, IOrchestratorLogger<TestingFrameworkOrchestrator> logger) : base(logger, storageManager)
        {
            this.logger = logger;
            this.translator = translator;
        }

        public async Task<string> EvaluateCorrectnessAsync(TranslationCorrectnessInput input)
        {
            var transcriptionOutputSegments = input.TranscriptionOutput.OrderBy((segment) => segment.SegmentID);
            var translationOutputSegments = input.TranslationOutput.OrderBy((segment) => segment.SegmentID);

            ValidateInput(input, transcriptionOutputSegments, translationOutputSegments);

            logger.LogInformation("Starting translation correctness evaluation...");
                        
            var threshold = input.Configuration.Threshold;
            var translatorConfig = input.Configuration.TranslatorConfiguration;
            var translationCorrectnessOutputSegments = new List<TranslationCorrectnessOutputSegment>();

            // Create re-translation input from translation output
            var retranslationInputSegments = new List<TranslatorInputSegment>();
            foreach (var translationSegment in translationOutputSegments)
            {
                var retranslationSegment = new TranslatorInputSegment(translationSegment.TranslatedText, translationSegment.TargetLocale, translationSegment.SourceLocale, translationSegment.SegmentID);
                retranslationInputSegments.Add(retranslationSegment);
            }

            // Translate the translated text back into its original text
            var retranslationInput = new TranslatorInput(translatorConfig, retranslationInputSegments);
            var retranslationSerializedOutput = await translator.DoTranslation(retranslationInput);
            var retranslatedSegments = JsonConvert.DeserializeObject<ICollection<TranslatorOutputSegment>>(retranslationSerializedOutput);

            for (var i = 0; i < retranslatedSegments.Count; i++)
            {
                var retranslatedSegment = retranslatedSegments.ElementAt(i);
                var transcriptionSegment = transcriptionOutputSegments.ElementAt(i);

                // Calculate the difference between the original source text and the re-translated text
                var difference = CalculateDifference(retranslatedSegment.TranslatedText, transcriptionSegment.DisplayText);

                // If the difference is over the threshold, flag this segment for intervention
                var translationCorrectnessSegment = new TranslationCorrectnessOutputSegment()
                {
                    SegmentID = transcriptionSegment.SegmentID,
                    InterventionNeeded = difference > threshold 
                };
                translationCorrectnessOutputSegments.Add(translationCorrectnessSegment);
            }

            return JsonConvert.SerializeObject(translationCorrectnessOutputSegments);
        }

        private void ValidateInput(TranslationCorrectnessInput input, IOrderedEnumerable<SpeechOutputSegment> transcriptionOutputSegments, IOrderedEnumerable<TranslatorOutputSegment> translationOutputSegments)
        {
            if (input == null)
            {
                logger.LogError($"Argument {nameof(input)} is null");
                throw new ArgumentNullException(nameof(input));
            }

            if (input.TranscriptionOutput == null)
            {
                logger.LogError($"Argument {nameof(input.TranscriptionOutput)} is null");
                throw new ArgumentNullException(nameof(input.TranscriptionOutput));
            }

            if (input.TranslationOutput == null)
            {
                logger.LogError($"Argument {nameof(input.TranslationOutput)} is null");
                throw new ArgumentNullException(nameof(input.TranslationOutput));
            }

            if (input.TranscriptionOutput.Count != input.TranslationOutput.Count)
            {
                logger.LogError("Output from transcription and translation must be the same length.");
                throw new ArgumentException("Output from transcription and translation must be the same length.");
            }

            if (input.Configuration == null)
            {
                logger.LogError($"Argument {nameof(input.Configuration)} is null");
                throw new ArgumentNullException(nameof(input.Configuration));
            }

            if (input.Configuration.Threshold > 1.0 || input.Configuration.Threshold < 0.0)
            {
                logger.LogError($"Argument {nameof(input.Configuration.Threshold)} must be a value between 0.0 and 1.0.");
                throw new ArgumentException($"{nameof(input.Configuration.Threshold)} must be a value between 0.0 and 1.0.");
            }

            for (var i = 0; i < transcriptionOutputSegments.Count(); i++)
            {
                var transcriptionOutputSegment = transcriptionOutputSegments.ElementAt(i);
                var translationOutputSegment = translationOutputSegments.ElementAt(i);

                if (transcriptionOutputSegment.SegmentID != translationOutputSegment.SegmentID)
                {
                    logger.LogError($"Transcription and translation segments don't match. Transcription segment with ID {transcriptionOutputSegment.SegmentID} " +
                        $"doesn't match translation segment with ID {translationOutputSegment.SegmentID}");
                    throw new ArgumentException($"Transcription and translation segments don't match. Transcription segment with ID {transcriptionOutputSegment.SegmentID} " +
                        $"doesn't match translation segment with ID {translationOutputSegment.SegmentID}");
                }

                if (string.IsNullOrEmpty(translationOutputSegment.TranslatedText) || string.IsNullOrEmpty(transcriptionOutputSegment.DisplayText))
                {
                    logger.LogError($"Transcription and translation segments must both contain a display text and a translated text respectively. Transcription segment with ID {transcriptionOutputSegment.SegmentID} " +
                        $"has display text {transcriptionOutputSegment.DisplayText} while the translation segment contians translated text {translationOutputSegment.TranslatedText}");
                    throw new ArgumentException($"Transcription and translation segments must both contain a display text and a translated text respectively. Transcription segment with ID {transcriptionOutputSegment.SegmentID} " +
                        $"has display text {transcriptionOutputSegment.DisplayText} while the translation segment contians translated text {translationOutputSegment.TranslatedText}"); ;
                }
            }
        }
        /// <summary>
        /// Calculate the difference between two given strings. A difference of 0.0 means the strings are identical, while a difference of 1.0 means the strings are completely different.
        /// </summary>
        /// <param name="string1">First string to compare.</param>
        /// <param name="string2">Second string to compare.</param>
        /// <returns>The difference between the two given strings.</returns>
        private double CalculateDifference(string string1, string string2)
        {
            var editDistance = CalculateEditDistance(string1, string2);
            double maxLength = Math.Max(string1.Length, string2.Length);

            return editDistance / maxLength;
        }

        /// <summary>
        /// Calculates the edit or Levenshtein distance in terms of the characters between the given strings using a dynamic programming approach.
        /// </summary>
        /// <param name="string1">First string to compare.</param>
        /// <param name="string2">Second string to compare.</param>
        /// <returns>The Levenshtein or edit distance between the two strings.</returns>
        private int CalculateEditDistance(string string1, string string2)
        {
            var len1 = string1.Length;
            var len2 = string2.Length;
            var results = new int[2, len1 + 1]; // Array to memoize result of previous computations

            // Base case: first string is empty, we remove all characters to get the second string
            for (var i = 0; i <= len1; i++)
            {
                results[0, i] = i;
            }

            for (int index2 = 1; index2 <= len2; index2++)
            {
                for (int index1 = 0; index1 <= len1; index1++)
                {                    
                    if (index1 == 0)
                    {
                        // Base case: second string is empty, we insert all characters to get the second string
                        results[index2 % 2, index1] = index2;
                    }                    
                    else if (string1[index1 - 1] == string2[index2 - 1])
                    {
                        // If character from both string is same then we do not perform any operation
                        results[index2 % 2, index1] = results[(index2 - 1) % 2, index1 - 1];
                    }                    
                    else
                    {
                        // If character from both String is not same then we take the minimum from three specified operation
                        var inserted = results[(index2 - 1) % 2, index1];
                        var missing = results[index2 % 2, index1 - 1];
                        var different = results[(index2 - 1) % 2, index1 - 1];
                        results[index2 % 2, index1] = 1 + Math.Min(Math.Min(missing, inserted), different);
                    }
                }
            }

            return results[len2 % 2, len1];
        }
    }
}
