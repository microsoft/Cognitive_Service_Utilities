using AIPlatform.TestingFramework.Utilities.JSON.Converters;
using Newtonsoft.Json;
using System;

namespace AIPlatform.TestingFramework.TTSPreProcessing
{
    public class TTSPreProcessingOutput
    {
        [JsonProperty("SegmentId")]
        public int SegmentId { get; set; }

        [JsonProperty("Duration")]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Duration { get; set; }

        [JsonProperty("Offset")]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Offset { get; set; }

        [JsonProperty("TargetLocale")]
        public string TargetLocale { get; set; }

        [JsonProperty("VoiceInfo")]
        public VoiceDetail VoiceInfo { get; set; }

        [JsonProperty("Ssml")]
        public string Ssml { get; set; }

        [JsonProperty("TextToSpeak")]
        public string TextToSpeak { get; set; }
    }
}
