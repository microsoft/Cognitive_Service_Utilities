using Newtonsoft.Json;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.SubtitlesGeneration
{
    public class SubtitlesWritingConfiguration
    {
        [JsonProperty("MaxNumberOfLines")]
        public int NumberOfLines { get; set; }

        [JsonProperty("NumberOfWordRange")]
        public List<int> NumberOfWordsRange { get; set; }

        [JsonProperty("GenerateSubtitleLevelTimestamps")]
        public bool GenerateSubtitleLevelTimestamps { get; set; }

        public override string ToString()
        {
            return $"NumberOfLines: {NumberOfLines}, NumberOfWordsRange: {NumberOfWordsRange}";
        }
    }
}
