//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Newtonsoft.Json;

namespace AIPlatform.TestingFramework.Evaluation.STT
{
    public class SpeechCorrectnessConfiguration
    {
        /// <summary>
        /// Sets the maximum percentage of top-confidence candidates that have a lower confidence score than the selected candidate
        /// to compare for a transcription segment. A threshold of 1 means all available candidates are used for comaparison.
        /// A threshold of 0 means only candidates with a confidence score higher than the selected candidate will be used for comparison.
        /// </summary>
        [JsonProperty("ConfidenceThreshold")]
        public double ConfidenceThreshold { get; set; }

        /// <summary>
        /// Sets the weight threshold for which points of contention are marked for correction.
        /// A threshold of 0 favors recall, ie. errors marked for correction in a segment must be present in at least one candidate of the segment.
        /// A threshold of 1 favors precision, ie. errors marked for correction must be present in all candidates of the segment.
        /// </summary>
        [JsonProperty("OccurrenceThreshold")]
        public double OccurrenceThreshold { get; set; }
    }
}
