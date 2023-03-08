using Newtonsoft.Json;

namespace AIPlatform.TestingFramework.TTSPreProcessing
{
    public class PreProcessTTSInput : PreProcessTTSInputSegment
    {
        [JsonProperty("TranslatedText")]
        public string TranslatedText { get; set; }

        [JsonProperty("SpeakingStyle")]
        public string SpeakingStyle { get; set; }

        [JsonProperty("Rate")]
        public double Rate { get; set; }

        [JsonProperty("HumanIntervention")]
        public bool HumanInterventionRequired { get; set; }

        [JsonProperty("HumanInterventionReason")]

        public string HumanInterventionReason { get; set; }

        [JsonProperty("VoiceInfo")]
        public VoiceDetail VoiceInfo { get; set; }

        [JsonProperty("PreProcessingStepConfig")]
        public TTSPreProcessingConfiguration PreProcessingStepConfig { get; set; }
    }
}
