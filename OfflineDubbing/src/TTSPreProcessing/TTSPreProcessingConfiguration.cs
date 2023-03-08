using AIPlatform.TestingFramework.Common;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AIPlatform.TestingFramework.TTSPreProcessing
{
    public class TTSPreProcessingConfiguration
    {
        [JsonProperty("ServiceConfiguration")]
        public CognitiveServiceConfiguration ServiceConfiguration { get; set; }

        [JsonProperty("VoiceMapping")]
        public Dictionary<string, VoiceDetail> VoiceMapping { get; set; }

        [JsonProperty("TranslationMappingMethod")]
        public TranslationMappingMethodEnum TranslationMappingMethod { get; set; }

        [DefaultValue(Double.MaxValue)]
        [JsonProperty("MaxSpeechRate", DefaultValueHandling = DefaultValueHandling.Populate)]
        public double MaxSpeechRate { get; set; }

        [DefaultValue(Double.MinValue)]
        [JsonProperty("MinSpeechRate", DefaultValueHandling = DefaultValueHandling.Populate)]
        public double MinSpeechRate { get; set; }
    }
}
