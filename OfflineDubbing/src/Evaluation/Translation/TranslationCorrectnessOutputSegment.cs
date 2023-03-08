//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Newtonsoft.Json;

namespace AIPlatform.TestingFramework.Evaluation.Translation
{
    public class TranslationCorrectnessOutputSegment
    {
        [JsonProperty("SegmentID")]
        public int SegmentID { get; set; }

        [JsonProperty("InterventionNeeded")]
        public bool InterventionNeeded { get; set; }
    }
}
