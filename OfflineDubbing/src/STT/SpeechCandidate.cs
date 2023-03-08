//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.STT.TranscriptionUtils;
using AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions;
using Newtonsoft.Json;
using System;

namespace AIPlatform.TestingFramework.STT
{
    public class SpeechCandidate
    {
        [JsonProperty("LexicalText", Required = Required.Always)]
        public string LexicalText { get; set; }

        [JsonProperty("Confidence", Required = Required.Always)]
        public float Confidence { get; set; }

        [JsonProperty("Words", Required = Required.Always)]
        public TimeStamp[] Words { get; set; }

        override
        public string ToString()
        {
            return $"{Environment.NewLine}{{{Environment.NewLine}\tLexicalText: {LexicalText},{Environment.NewLine}\tConfidence: {Confidence}," +
                $"{Environment.NewLine}\tWords: {Words.ToJSONArray().Indent()}{Environment.NewLine}}}";
        }
    }
}
