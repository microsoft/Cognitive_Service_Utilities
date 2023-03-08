//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Pipeline.Configuration;
using Newtonsoft.Json;

namespace AIPlatform.TestingFramework.Pipeline.Execution
{
    public class PipelineStatus : PostBody
    {
        /// <summary>
        /// Used to store current pipeline stepId during pipeline execution
        /// </summary>
        [JsonProperty("CurrentPipelineStepId")]
        public string CurrentPipelineStepId { get; set; }

        /// <summary>
        /// Used to store previously executed pipeline stepid during pipeline execution
        /// </summary>
        [JsonProperty("PreviousPipelineStepId")]
        public string PreviousPipelineStepId { get; set; }

    }
}
