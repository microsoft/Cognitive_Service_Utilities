using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions;
using System;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.PostProcessingSTT
{
    public class PostProcessSTTInput
    {
        public PostProcessSTTConfiguration PostProcessingSTTStepConfiguration;

        public ICollection<SpeechOutputSegment> Input;

        public PostProcessSTTInput(PostProcessSTTConfiguration postProcessingSTTStepConfiguration, ICollection<SpeechOutputSegment> input)
        {
            PostProcessingSTTStepConfiguration = postProcessingSTTStepConfiguration;
            Input = input;
        }

        override
        public string ToString()
        {
            return $"{Environment.NewLine}{{{Environment.NewLine}\tPostProcessingSTTStepConfiguration: " +
                $"{PostProcessingSTTStepConfiguration.ToString().Indent()}," +
                $"{Environment.NewLine}\tInput: {Input.ToJSONArray().Indent()}{Environment.NewLine}}}";
        }
    }
}
