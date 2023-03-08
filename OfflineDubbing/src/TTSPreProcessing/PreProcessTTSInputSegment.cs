using AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions;
using AIPlatform.TestingFramework.Utilities.JSON.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.TTSPreProcessing
{
    public class PreProcessTTSInputSegment
    {
        public class TimeStamp
        {
            [JsonProperty("Word", Required = Required.Always)]
            public string Word { get; set; }

            [JsonProperty("Duration", Required = Required.Always)]
            [JsonConverter(typeof(TimeSpanConverter))]
            public TimeSpan Duration { get; set; }

            [JsonProperty("Offset", Required = Required.Always)]
            [JsonConverter(typeof(TimeSpanConverter))]
            public TimeSpan Offset { get; set; }

            override
            public string ToString()
            {
                return $"{Environment.NewLine}{{{Environment.NewLine}\tWord: {Word}," +
                    $"{Environment.NewLine}\tDuration: {Duration}," +
                    $"{Environment.NewLine}\tOffset: {Offset}{Environment.NewLine}}}";
            }
        }

        [JsonProperty("LexicalText", Required = Required.Always)]
        public string LexicalText { get; set; }

        [JsonProperty("DisplayText", Required = Required.Always)]
        public string DisplayText { get; set; }

        [JsonProperty("IdentifiedLocale", Required = Required.Always)]
        public string IdentifiedLocale { get; set; }

        [JsonProperty("IdentifiedSpeaker", Required = Required.Default)]
        public string IdentifiedSpeaker { get; set; }

        [JsonProperty("IdentifiedEmotion", Required = Required.Default)]
        public string IdentifiedEmotion { get; set; }

        [JsonProperty("SegmentID", Required = Required.Always)]
        public int SegmentID { get; set; }

        [JsonProperty("Duration", Required = Required.Always)]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Duration { get; set; }

        [JsonProperty("Offset", Required = Required.Always)]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Offset { get; set; }

        [JsonProperty("TimeStamps", Required = Required.Always)]
        public ICollection<TimeStamp> TimeStamps { get; set; }

        [JsonProperty("TargetLocale", Required = Required.Always)]
        public string TargetLocale { get; set; }

        override
        public string ToString()
        {
            return $"{Environment.NewLine}{{{Environment.NewLine}\tSegmentID: {SegmentID},{Environment.NewLine}\tLexicalText: {LexicalText}," +
                $"{Environment.NewLine}\tDisplayText: {DisplayText},{Environment.NewLine}\tIdentifiedSpeaker: {IdentifiedSpeaker}" +
                $"{Environment.NewLine}\tIdentifiedLocale: {IdentifiedLocale},{Environment.NewLine}\tIdentifiedEmotion: {IdentifiedEmotion}," +
                $"{Environment.NewLine}\tDuration: {Duration},{Environment.NewLine}\tOffset: {Offset}," +
                $"{Environment.NewLine}\tTargetLocale: {TargetLocale}" +
                $"{Environment.NewLine}\tTimeStamps: {TimeStamps.ToJSONArray().Indent()}" +
                $"{Environment.NewLine}}}";
        }
    }
}
