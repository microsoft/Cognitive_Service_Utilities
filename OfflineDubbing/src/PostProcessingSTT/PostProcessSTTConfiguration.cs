using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.PostProcessingSTT
{
    public class PostProcessSTTConfiguration
    {
        [JsonProperty("SourceLocales")]
        public ICollection<string> SourceLocales { get; set; }

        [JsonProperty("TargetLocale")]
        public string TargetLocale { get; set; }

        [JsonProperty("IgnoreUnexpectedSourceLocales")]
        public bool IgnoreUnexpectedSourceLocales { get; set; }

        [JsonProperty("ConcatenateMatchingSegments")]
        public bool ConcatenateMatchingSegments { get; set; }

        public PostProcessSTTConfiguration()
        {
            ConcatenateMatchingSegments = true;
        }

        override
        public string ToString()
        {
            return $"{{ {Environment.NewLine}\tSourceLocales: {string.Join(", ", SourceLocales)}," +
                $"{Environment.NewLine}\tTargetLocale: {TargetLocale}," +
                $"{Environment.NewLine}\tConcatenateMatchingSegments: {ConcatenateMatchingSegments}," +
                $"{Environment.NewLine}\tIgnoreUnexpectedSourceLocales: {IgnoreUnexpectedSourceLocales}{Environment.NewLine}}}";
        }
    }
}
