using System;

namespace AIPlatform.TestingFramework.TTSPreProcessing
{
    public class VoiceDetail
    {
        public string VoiceName { get; set; }

        public string EndpointKey { get; set; }

        override
        public string ToString()
        {
            return $"{Environment.NewLine}\tVoiceModelName: {VoiceName}{Environment.NewLine}";
        }
    }
}
