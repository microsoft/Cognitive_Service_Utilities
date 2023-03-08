using Newtonsoft.Json;
using System;

namespace AIPlatform.TestingFramework.Translation
{
    public class TranslatorOutputSegment : TranslatorSegment
    {
        [JsonProperty("TranslatedText", Required = Required.Always)]
        public string TranslatedText { get; set; }

        /// <summary>
        /// Constructor for Translator output segment.
        /// </summary>
        /// <param name="translatedText">The translated text for this segment.</param>
        /// <param name="sourceLocale">The locale this segment was translated from.</param>
        /// <param name="targetLocale">The locale this segment was translated into.</param>
        /// <param name="segmentID">The segment ID if the output is segmented.</param>
        public TranslatorOutputSegment(string translatedText, string sourceLocale, string targetLocale,  int segmentID = -1) 
            : base(sourceLocale, targetLocale, segmentID)
        {
            TranslatedText = translatedText;
        }

        override
        public string ToString()
        {
            var str = $"{Environment.NewLine}{{{Environment.NewLine}\tTranslatedText: {TranslatedText}";

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
