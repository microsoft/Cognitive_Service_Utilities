using AIPlatform.TestingFramework.Utilities.JSON.Converters;
using Newtonsoft.Json;
using System;

namespace AIPlatform.TestingFramework.STT.TranscriptionUtils
{

    public class DetailedTranscriptionOutputResultSegment
    {
        [JsonProperty("Id")]
        public string Id { get; set; }

        [JsonProperty("RecognitionStatus")]
        public string RecognitionStatus { get; set; }

        [JsonProperty("Offset")]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Offset { get; set; }

        [JsonProperty("Duration")]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Duration { get; set; }

        [JsonProperty("PrimaryLanguage")]
        public Primarylanguage PrimaryLanguage { get; set; }

        [JsonProperty("DisplayText")]
        public string DisplayText { get; set; }

        [JsonProperty("NBest")]
        public NBest[] NBest { get; set; }

        override
        public string ToString()
        {
            return $"{Environment.NewLine}{{{Environment.NewLine}\tId: {Id},{Environment.NewLine}\tRecognitionStatus: {RecognitionStatus},{Environment.NewLine}\tOffset: {Offset}," +
                $"{Environment.NewLine}\tDuration: {Duration},{Environment.NewLine}\tPrimaryLanguage: {PrimaryLanguage},{Environment.NewLine}\tDisplayText: {DisplayText},{Environment.NewLine}\tNBest: {NBest}}}";
        }
    }

    public class NBest
    {
        [JsonProperty("Confidence")]
        public float Confidence { get; set; }

        [JsonProperty("Lexical")]
        private string Lexical { set { LexicalText = value; } }

        [JsonProperty("LexicalText")]
        public string LexicalText { get; set; }

        [JsonProperty("DisplayText")]
        public string DisplayText { get; set; }

        [JsonProperty("ITN")]
        public string ITN { get; set; }

        [JsonProperty("MaskedITN")]
        public string MaskedITN { get; set; }

        [JsonProperty("Display")]
        private string Display { set { DisplayText = value; } }

        [JsonProperty("Words")]
        public TimeStamp[] Words { get; set; }

        [JsonProperty("DisplayWords")]
        public TimeStamp[] DisplayWords { get; set; }

        override
        public string ToString()
        {
            return $"{Environment.NewLine}{{{Environment.NewLine}\tConfidence: {Confidence},{Environment.NewLine}\tLexical: {LexicalText},{Environment.NewLine}\tITN: {ITN}," +
                $"{Environment.NewLine}\tMaskedITN: {MaskedITN},{Environment.NewLine}\tDisplay: {DisplayText},{Environment.NewLine}\tWords: {Words},{Environment.NewLine}\tDisplayWords: {DisplayWords}}}";
        }
    }

    public class TimeStamp
    {
        public TimeStamp(string word, TimeSpan duration, TimeSpan offset)
        {
            Word = word;
            Duration = duration;
            Offset = offset;
        }

        [JsonProperty("Word", Required = Required.Always)]
        public string Word { get; set; }

        [JsonProperty("Offset", Required = Required.Always)]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Offset { get; set; }

        [JsonProperty("Duration", Required = Required.Always)]
        [JsonConverter(typeof(TimeSpanConverter))]
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Creates and returns deep copy of this TimeStamp.
        /// </summary>
        /// <returns>Deep copy of this TimeStamp.</returns>
        public TimeStamp Copy()
        {
            return new TimeStamp(Word, Duration, Offset);
        }

    override
        public string ToString()
        {
            return $"{Environment.NewLine}{{{Environment.NewLine}\tText: {Word},{Environment.NewLine}\tOffset: {Offset},{Environment.NewLine}\tDuration: {Duration}{Environment.NewLine}}}";
        }
    }

    public class Primarylanguage
    {
        [JsonProperty("Language")]
        public string Language { get; set; }

        [JsonProperty("Confidence")]
        public string Confidence { get; set; }

        override
        public string ToString()
        {
            return $"{Environment.NewLine}{{{Environment.NewLine}\tLanguage: {Language},{Environment.NewLine}\tConfidence: {Confidence}}}";
        }
    }
}
