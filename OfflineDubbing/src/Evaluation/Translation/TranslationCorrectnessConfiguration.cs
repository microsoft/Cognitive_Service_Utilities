//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Translation;
using Newtonsoft.Json;

namespace AIPlatform.TestingFramework.Evaluation.Translation
{
    public class TranslationCorrectnessConfiguration
    {
        /// <summary>
        /// Must be between 0.0 and 1.0. Determines the maximum difference allowed between the expected and actual translation result using bi-directional translation.
        /// The translation output will be translated back from the target language into the source language, resulting in two strings: the original source (Si) and the expected
        /// value of source E(Si). These two strings are compared using the number of insertions, edits and deletions between them.
        /// If the percent difference of source (Si) compared to the expected source E(Si) is within the threshold, then it is considered an identical match
        /// and the segment is considered well translated. Otherwise, the segment will be flagged for human intervention.
        /// </summary>
        [JsonProperty("Threshold")]
        public double Threshold { get; set; }

        /// <summary>
        /// The translator configuration we'll use to translate the translation results back into the source language. Since we'll compare the results of this to
        /// to the result of the translation to determine the confidence of the translation results, this configuration should be the same as the one used for the translation step.
        /// </summary>
        [JsonProperty("TranslatorConfiguration")]
        public TranslatorConfiguration TranslatorConfiguration { get; set; }
    }
}
