//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Common;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.Evaluation.STT
{
    public class SpeechCorrectnessInput
    {
        [JsonProperty("Configuration")]
        public SpeechCorrectnessConfiguration Configuration { get; set; }

        [JsonProperty("Input")]
        public ICollection<SpeechOutputSegment> Input { get; set; }

        public SpeechCorrectnessInput(SpeechCorrectnessConfiguration configuration, ICollection<SpeechOutputSegment> input)
        {
            this.Configuration = configuration;
            this.Input = input;
        }
    }
}
