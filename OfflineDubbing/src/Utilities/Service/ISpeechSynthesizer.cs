//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;

namespace AIPlatform.TestingFramework.Utilities.Service
{
    public interface ISpeechSynthesizer
    {
        public SpeechResult SpeakTextAsync(string text);

        public SpeechResult SpeakSsmlAsync(string SSML);
    }

    public class SpeechResult
    {
        public SpeechResult(byte[] audioData, TimeSpan audioDuration)
        {
            this.AudioData = audioData;
            this.AudioDuration = audioDuration;
        }

        public byte[] AudioData { get; set; }
        public TimeSpan AudioDuration { get; set; }
    }
}
