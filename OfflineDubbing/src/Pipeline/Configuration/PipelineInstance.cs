using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.Pipeline.Configuration
{
    public class PipelineInstance
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("PipelineSteps")]
        public List<PipelineStep> PipelineSteps { get; set; }

    }
}
