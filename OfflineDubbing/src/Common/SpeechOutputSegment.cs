﻿using AIPlatform.TestingFramework.STT;
using AIPlatform.TestingFramework.STT.TranscriptionUtils;
using AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions;
using AIPlatform.TestingFramework.Utilities.JSON.Converters;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.Common
{
    public class SpeechOutputSegment
    {
        [JsonProperty("LexicalText", Required = Required.Always)]
        public string LexicalText { get; set; }

        [JsonProperty("DisplayText", Required = Required.Always)]
        public string DisplayText { get; set; }

        [JsonProperty("IdentifiedSpeaker", Required = Required.Default)]
        public string IdentifiedSpeaker { get; set; }

        [JsonProperty("IdentifiedLocale", Required = Required.Always)]
        public string IdentifiedLocale { get; set; }

        [JsonProperty("IdentifiedEmotion", Required = Required.Default)]
        public string IdentifiedEmotion { get; set; }

        /// <summary>
        /// This is duration of the detected speech audio
        /// </summary>
        [JsonProperty("Duration", Required = Required.Always)]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// This is from the start of the audio
        /// </summary>
        [JsonProperty("Offset", Required = Required.Always)]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Offset { get; set; }

        [JsonProperty("SegmentID", Required = Required.Always)]
        public int SegmentID { get; set; }

        [JsonProperty("TimeStamps", Required = Required.Always)]
        public ICollection<TimeStamp> TimeStamps { get; set; }

        [JsonProperty("DisplayWordTimeStamps", Required = Required.Always)]
        public ICollection<TimeStamp> DisplayWordTimeStamps { get; set; }

        [JsonProperty("NBest", Required = Required.AllowNull)]
        public ICollection<SpeechCandidate> NBest { get; set; }

        override
        public string ToString()
        {
            return $"{Environment.NewLine}{{{Environment.NewLine}\tSegmentID: {SegmentID},{Environment.NewLine}\tLexicalText: {LexicalText}," +
                $"{Environment.NewLine}\tDisplayText: {DisplayText},{Environment.NewLine}\tIdentifiedSpeaker: {IdentifiedSpeaker}" +
                $"{Environment.NewLine}\tIdentifiedLocale: {IdentifiedLocale},{Environment.NewLine}\tIdentifiedEmotion: {IdentifiedEmotion}" +
                $"{Environment.NewLine}\tDuration: {Duration},{Environment.NewLine}\tOffset: {Offset}," +
                $"{Environment.NewLine}\tTimeStamps: {TimeStamps.ToJSONArray().Indent()},{Environment.NewLine}," +
                $"{Environment.NewLine}\tNBest: {NBest.ToJSONArray().Indent()}" +
                $"{Environment.NewLine}}}";
        }

        public SpeechOutputSegment()
        {
            TimeStamps = new List<TimeStamp>();
            DisplayWordTimeStamps = new List<TimeStamp>();
            NBest = new List<SpeechCandidate>();
        }
    }
}
