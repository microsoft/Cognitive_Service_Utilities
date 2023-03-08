using AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions;
using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Evaluation.STT;
using System.Collections.Generic;
using System;

namespace AIPlatform.TestingFramework.SubtitlesGeneration
{
    public class SubtitlesWritingInput: SpeechOutputSegment
    {
        public bool HumanIntervention { get; set; }

        public ICollection<SpeechPointOfContention> HumanInterventionReasons { get; set; }

        override public string ToString()
        {
            return $"{Environment.NewLine}{{{Environment.NewLine}\tSegmentID: {SegmentID},{Environment.NewLine}\tLexicalText: {LexicalText}," +
                $"{Environment.NewLine}\tDisplayText: {DisplayText},{Environment.NewLine}\tIdentifiedSpeaker: {IdentifiedSpeaker}" +
                $"{Environment.NewLine}\tIdentifiedLocale: {IdentifiedLocale},{Environment.NewLine}\tIdentifiedEmotion: {IdentifiedEmotion}" +
                $"{Environment.NewLine}\tDuration: {Duration},{Environment.NewLine}\tOffset: {Offset}," +
                $"{Environment.NewLine}\tTimeStamps: {DisplayWordTimeStamps.ToJSONArray().Indent()}" +
                $"{Environment.NewLine}\tHumanIntervention: {HumanIntervention}, " +
                $"{Environment.NewLine}\tHumanInterventionReasons: {HumanInterventionReasons.ToJSONArray().Indent()}, " +
                $"{Environment.NewLine}}}";
        }
    }
}
