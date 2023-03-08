//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.Pipeline.Configuration
{
    public class PipelineStep
    {
        [JsonProperty("StepOrder")]
        public int StepOrder { get; set; }

        [JsonProperty("StepName")]
        public StepNameEnum StepName { get; set; }

        [JsonProperty("StepID")]
        public string StepID { get; set; }

        [JsonProperty("Inputs")]
        public List<string> Inputs { get; set; }

        [JsonProperty("StepConfig")]
        public JObject StepConfig { get; set; }

        //TODO: Review if we need this property
        [JsonProperty("OutputType")]
        public OutputTypeEnum OutputType { get; set; }

        [JsonProperty("Outputs")]
        public List<string> Outputs { get; set; }

        [JsonProperty("ExecutionMode")]
        public StepExecutionModeEnum ExecutionMode { get; set; }

        [JsonProperty("PipelineSteps")]
        public List<PipelineStep> PipelineSteps { get; set; }

        public PipelineStep()
        {
            ExecutionMode = StepExecutionModeEnum.Series;
            Inputs = new List<string>();
            Outputs = new List<string>();
        }
    }
}
