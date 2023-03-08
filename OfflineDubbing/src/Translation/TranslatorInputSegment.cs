//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Newtonsoft.Json;
using System;

namespace AIPlatform.TestingFramework.Translation
{
    public class TranslatorInputSegment : TranslatorSegment
    {
        [JsonProperty("SegmentID", Required = Required.Always)]
        public int SegmentID { get; set; }

        [JsonProperty("SourceText", Required = Required.Always)]
        public string SourceText { get; set; }

        /// <summary>
        /// Constructor for Translator input segment.
        /// </summary>
        /// <param name="sourceText">Text input to translate.</param>
        /// <param name="sourceLocale">The source locale to translate from.</param>
        /// <param name="targetLocale">The target locale to translate to.</param>
        /// <param name="segmentId">The segment ID if the input is segmented.</param>
        public TranslatorInputSegment(string sourceText, string sourceLocale = null, string targetLocale = null, int segmentId = -1) 
            : base(sourceLocale, targetLocale, segmentId)
        {
            SourceText = sourceText;
            SourceLocale = sourceLocale;
            TargetLocale = targetLocale;
            SegmentID = segmentId;
        }

        override
        public string ToString()
        {
            var str = $"{Environment.NewLine}{{{Environment.NewLine}\tSourceText: {SourceText}";

            if (!string.IsNullOrEmpty(SourceLocale))
            {
                str += $",{Environment.NewLine}\tSourceLocale: {SourceLocale}";
            }

            if (!string.IsNullOrEmpty(TargetLocale))
            {
                str += $",{Environment.NewLine}\tTargetLocale: {TargetLocale}";
            }

            if (SegmentID >= 0)
            {
                str += $",{Environment.NewLine}\tSegmentId: {SegmentID}";
            }

            str += $"{Environment.NewLine}}}";

            return str;
        }
    }
}
