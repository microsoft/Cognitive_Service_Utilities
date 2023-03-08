using Newtonsoft.Json;
using System;

namespace AIPlatform.TestingFramework.Common
{
    public class CognitiveServiceConfiguration
    {
        [JsonProperty("Region")]
        public string Region { get; set; }

        [JsonProperty("SubscriptionKey")]
        public string SubscriptionKey { get; set; }

        override
        public string ToString()
        {
            return $"{{ {Environment.NewLine}\tSubscriptionKey: Redacted, {Environment.NewLine}\t, {Environment.NewLine}\tRegion: {Region}, {Environment.NewLine}}}";
        }
    }
}
