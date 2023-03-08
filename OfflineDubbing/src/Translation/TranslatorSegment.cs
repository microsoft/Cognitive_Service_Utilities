//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Newtonsoft.Json;

namespace AIPlatform.TestingFramework.Translation
{
    public abstract class TranslatorSegment
    {
        [JsonProperty("SegmentID", Required = Required.Always)]
        public int SegmentID { get; set; }

        [JsonProperty("SourceLocale", Required = Required.Default)]
        public string SourceLocale { get; set; }

        [JsonProperty("TargetLocale", Required = Required.Default)]
        public string TargetLocale { get; set; }

        /// <summary>
        /// Constructor for Translator segment.
        /// </summary>
        /// <param name="sourceLocale">The source locale of the translation.</param>
        /// <param name="targetLocale">The target locale of the translation.</param>
        /// <param name="segmentId">The segment ID if the input is segmented.</param>
        public TranslatorSegment(string sourceLocale = null, string targetLocale = null, int segmentId = -1)
        {
            SourceLocale = sourceLocale;
            TargetLocale = targetLocale;
            SegmentID = segmentId;
        }
    }
}
