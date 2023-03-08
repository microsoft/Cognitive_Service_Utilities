using AIPlatform.TestingFramework.Evaluation.STT;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIPlatform.TestingFramework.SubtitlesGeneration
{
    public class WebVttSubtitle
    {
        [JsonProperty("SubtitleId", Required = Required.Always)]
        public int SubtitleId { get; set; }

        [JsonProperty("SubtitleText", Required = Required.Always)]
        public SubtitleText SubtitleText { get; set; }

        [JsonProperty("Timestamps", Required = Required.Always)]
        public WebVttTimestamps Timestamps { get; set; }

        [JsonProperty("SubtitleMetadata", Required = Required.Default)]
        public SubtitleMetadata SubtitleMetadata { get; set; }

        public override string ToString()
        {
            var str = $"{Environment.NewLine}{Environment.NewLine}";
            str += SubtitleMetadata != null ? $"{SubtitleMetadata}{Environment.NewLine}{Environment.NewLine}" : "";
            str += $"{SubtitleId}{Environment.NewLine}{Timestamps}{Environment.NewLine}{SubtitleText}";
            return str;
        }
    }

    public class WebVttTimestamps
    {
        public WebVttTimestamps(TimeSpan start, TimeSpan end)
        {
            Start = start;
            End = end;
        }

        [JsonProperty("Start", Required = Required.Always)]
        public TimeSpan Start { get; set; }

        [JsonProperty("End", Required = Required.Always)]
        public TimeSpan End { get; set; }

        public override string ToString() => $"{this.Start:hh\\:mm\\:ss\\.fff} --> {this.End:hh\\:mm\\:ss\\.fff}";
    }

    public class SubtitleMetadata
    {
        [JsonProperty("Locale", Required = Required.Default)]
        public string Locale { get; set; }

        [JsonProperty("Speaker", Required = Required.Default)]
        public string Speaker { get; set; }

        [JsonProperty("HumanIntervention", Required = Required.Always)]
        public bool HumanIntervention { get; set; }

        [JsonProperty("HumanInterventionReasons", Required = Required.Default)]
        public ICollection<SpeechPointOfContention> HumanInterventionReasons { get; set; }

        public string GetSerializedMetadata()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        public override string ToString()
        {
            return $"NOTE{Environment.NewLine}{GetSerializedMetadata()}";
        }
    }

    public class SubtitleText
    {
        public SubtitleText(List<string> lines)
        {
            Lines = lines;
        }

        [JsonProperty("Lines", Required = Required.Always)]
        public List<string> Lines { get; set; }

        public int GetLinesCount()
        {
            return Lines.Count;
        }

        public override string ToString()
        {
            var last = Lines.Last();
            var lastStr = $"-{last}";
            return string.Join("", Lines.Select(line => $"-{line}{Environment.NewLine}").SkipLast(1).Append(lastStr));
        }
    }
}
