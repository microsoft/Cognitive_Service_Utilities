namespace AIPlatform.TestingFramework.STT
{
    public class SpeechInput
    {
        public SpeechConfiguration StepConfiguration { get; set; }

        public string Input { get; set; }

        public SpeechInput(SpeechConfiguration stepConfiguration, string input)
        {
            StepConfiguration = stepConfiguration;
            Input = input;
        }
    }
}
