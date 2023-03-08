//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Translation;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.Evaluation.Translation
{
    public class TranslationCorrectnessInput
    {
        [JsonProperty("TranscriptionOutput")]
        public ICollection<SpeechOutputSegment> TranscriptionOutput { get; set; }

        [JsonProperty("TranslationOutput")]
        public ICollection<TranslatorOutputSegment> TranslationOutput {  get; set; }

        [JsonProperty("Configuration")]
        public TranslationCorrectnessConfiguration Configuration { get; set; }

        public TranslationCorrectnessInput(
            TranslationCorrectnessConfiguration configuration,
            ICollection<SpeechOutputSegment> transcriptionOutput,
            ICollection<TranslatorOutputSegment> translationOutput
            )
        { 
            Configuration = configuration;
            TranscriptionOutput = transcriptionOutput;
            TranslationOutput = translationOutput;
        }
    }
}
