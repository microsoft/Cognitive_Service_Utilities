using System.Collections.Generic;

namespace AIPlatform.TestingFramework.SubtitlesGeneration
{
    public class SubtitlesWritingConfigInput
    {
        public List<SubtitlesWritingInput> Inputs { get; set; }

        public SubtitlesWritingConfiguration Config;

        public SubtitlesWritingConfigInput(List<SubtitlesWritingInput> inputs, SubtitlesWritingConfiguration config)
        {
            this.Inputs = inputs;
            this.Config = config;
        }
    }
}
