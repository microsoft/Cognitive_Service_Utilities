using Newtonsoft.Json;

namespace AIPlatform.TestingFramework.TTS
{
    public class TextToSpeechInput
    {
        [JsonProperty("TTSInput")]
        public string TTSInput { get; set; }

        [JsonProperty("StepConfiguration")]
        public TextToSpeechConfiguration StepConfiguration { get; set; }

        public TextToSpeechInput(TextToSpeechConfiguration stepConfiguration, string input)
        {
            StepConfiguration = stepConfiguration;
            this.TTSInput = input;
        }
    }
}
