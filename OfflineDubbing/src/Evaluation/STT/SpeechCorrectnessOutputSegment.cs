//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Evaluation.STT;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.Evaluation.Interfaces
{
    public class SpeechCorrectnessOutputSegment
    {
        [JsonProperty("SegmentID", Required = Required.Always)]
        public int SegmentID { get; set; }

        [JsonProperty("InterventionNeeded", Required = Required.Always)]
        public bool InterventionNeeded { get; set; }

        [JsonProperty("InterventionReasons", Required = Required.Default)]
        public ICollection<SpeechPointOfContention> InterventionReasons { get; set; }
    }
}
