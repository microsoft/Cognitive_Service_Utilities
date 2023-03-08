//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Collections.Generic;
using Newtonsoft.Json;

namespace AIPlatform.TestingFramework.Pipeline.Configuration
{
    public class PostBody
    {
        [JsonProperty("Input")]
        public string Input { get; set; }

        [JsonProperty("ExpectedOutput")]
        public string ExpectedOutput { get; set; }

        [JsonProperty("Pipeline")]
        public PipelineInstance ExecutionPipeline { get; set; }

        [JsonProperty("Dataset")]
        public Dictionary<string, string> Dataset { get; set; }

        public PostBody()
        {
            Dataset = new Dictionary<string, string>();
        }
    }
}
