//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using AIPlatform.TestingFramework.TTSPreProcessing;
using System.Xml.Linq;
using System.Xml;
using AIPlatform.TestingFramework.Utilities.Service;

namespace AIPlatform.EvaluationFramework.Test.Unit
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class PreprocessTTSTest
    {
        private static readonly double defaultSpeechRate = 1.0;
        [TestMethod]
        public void GetRelativeTargetRate_ReturnsDefault_If_UnsupportedLocale()
        {
            // setup test
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();

            var preProcessTTS = new PreProcessTTS(loggerMock.Object, new TestUnitSpeechSynthesizer());

            var expectedOutput = $@"<?xml version=""1.0""?>
                                    <speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xmlns:mstts=""https://www.w3.org/2001/mstts"" xml:lang=""en-US"">
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""0ms""/>
                                        <prosody rate=""{defaultSpeechRate}"">This is segment 1</prosody>
                                      </voice>
                                    </speak>";

            expectedOutput = XDocument.Parse(expectedOutput.Replace("\n", "")).ToString();

            //create input
            List<PreProcessTTSInput> testInput = new List<PreProcessTTSInput>();

            PreProcessTTSInput segment1 = new PreProcessTTSInput
            {
                TranslatedText = "This is segment 1",
                IdentifiedLocale = "hi-IN",
                TargetLocale = "en-US",
                SegmentID = 1,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 0),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment1.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorMiddle;
            segment1.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            testInput.Add(segment1);

            // run test

            var actualOutput = XDocument.Parse(preProcessTTS.DoTTSPreProcessing(testInput)).ToString();

            Assert.AreEqual(expectedOutput.Replace("\n", ""), actualOutput.Replace("\n", ""));
        }

        [TestMethod]
        public void GetRelativeTargetRate_ReturnsRelativeRate_ForSupportedLanguages()
        {
            // setup test
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();

            var preProcessTTS = new PreProcessTTS(loggerMock.Object, new TestUnitSpeechSynthesizer());

            var expectedOutput = @"<?xml version=""1.0""?>
                                    <speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xmlns:mstts=""https://www.w3.org/2001/mstts"" xml:lang=""en-US"">
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""0ms""/>
                                        <prosody rate=""1.0458355054737938"">Este es el segmento 1</prosody>
                                      </voice>
                                    </speak>";

            expectedOutput = XDocument.Parse(expectedOutput.Replace("\n", "")).ToString();

            //create input
            List<PreProcessTTSInput> testInput = new List<PreProcessTTSInput>();

            PreProcessTTSInput segment1 = new PreProcessTTSInput
            {
                TranslatedText = "Este es el segmento 1",
                LexicalText = "This is segment 1",
                IdentifiedLocale = "en-US",
                TargetLocale = "es-MX",
                SegmentID = 1,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 0),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment1.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorMiddle;
            segment1.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment1.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment1.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment1);

            // run test

            var actualOutput = XDocument.Parse(preProcessTTS.DoTTSPreProcessing(testInput)).ToString();

            Assert.AreEqual(expectedOutput.Replace("\n", ""), actualOutput.Replace("\n", ""));
        }

        [TestMethod]
        public void CompensatePausesAnchorMiddle_TargetFitsInTimeSpan()
        {
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();

            var durationDictionary = new Dictionary<string, int>
            {
                { "यह खंड 1 है", 1500 },
                { "यह खंड 2 है", 1500 },
                { "यह खंड 3 है", 1500 },
                { "यह खंड 4 है", 1500 }
            };

            var preProcessTTS = new PreProcessTTS(loggerMock.Object, new MockSpeechSynthesizer(durationDictionary));

            var expectedOutput = $@"<?xml version=""1.0""?>
                                    <speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xmlns:mstts=""https://www.w3.org/2001/mstts"" xml:lang=""en-US"">
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""0ms""/>
                                        <prosody rate=""{defaultSpeechRate}"">यह खंड 1 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""250ms""/>
                                        <prosody rate=""{defaultSpeechRate}"">यह खंड 2 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""500ms""/>
                                        <prosody rate=""{defaultSpeechRate}"">यह खंड 3 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""500ms""/>
                                        <prosody rate=""{defaultSpeechRate}"">यह खंड 4 है</prosody>
                                      </voice>
                                    </speak>";

            expectedOutput = XDocument.Parse(expectedOutput.Replace("\n", "")).ToString();

            //create input
            List<PreProcessTTSInput> testInput = new List<PreProcessTTSInput>();

            PreProcessTTSInput segment1 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 1 है",
                LexicalText = "This is segment 1",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 1,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 0),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment1.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorMiddle;
            segment1.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment1.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment1.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment1);

            PreProcessTTSInput segment2 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 2 है",
                LexicalText = "This is segment 2",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 2,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 2),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment2.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorMiddle;
            segment2.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment2.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment2.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment2);

            PreProcessTTSInput segment3 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 3 है",
                LexicalText = "This is segment 3",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 3,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 4),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment3.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorMiddle;
            segment3.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment3.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment3.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment3);

            PreProcessTTSInput segment4 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 4 है",
                LexicalText = "This is segment 4",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 4,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 6),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment4.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorMiddle;
            segment4.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment4.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment4.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment4);

            // run test

            var actualOutput = XDocument.Parse(preProcessTTS.DoTTSPreProcessing(testInput)).ToString();

            Assert.AreEqual(expectedOutput.Replace("\n", ""), actualOutput.Replace("\n", ""));
        }

        [TestMethod]
        public void CompensatePausesAnchorMiddle_TargetDoesNotFitsInTimeSpanSpeedUp()
        {
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();

            var durationDictionary = new Dictionary<string, int>
            {
                { "यह खंड 1 है", 2000 },
                { "यह खंड 2 है", 2000 },
                { "यह खंड 3 है", 2000 },
                { "यह खंड 4 है", 2000 }
            };

            var preProcessTTS = new PreProcessTTS(loggerMock.Object, new MockSpeechSynthesizer(durationDictionary));

            var expectedOutput = @"<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xmlns:mstts=""https://www.w3.org/2001/mstts"" xml:lang=""en-US"">
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms"" />
                                        <mstts:silence type=""Leading"" value=""0ms"" />
                                        <break time=""0ms"" />
                                        <prosody rate=""1"">यह खंड 1 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms"" />
                                        <mstts:silence type=""Leading"" value=""0ms"" />
                                        <break time=""250ms"" />
                                        <prosody rate=""1.375"">यह खंड 2 है</prosody>
                                        <!-- Warning: Intervention Requied - After translation this segment overlapped with previous segment, also the pause for this segment in the source was relatively large (and yet the translated segment did not fit), hence this segment was sped up and placed after the previous segment with a small pause -->
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms"" />
                                        <mstts:silence type=""Leading"" value=""0ms"" />
                                        <break time=""250ms"" />
                                        <prosody rate=""1.227"">यह खंड 3 है</prosody>
                                        <!-- Warning: Intervention Requied - After translation this segment overlapped with previous segment, also the pause for this segment in the source was relatively large (and yet the translated segment did not fit), hence this segment was sped up and placed after the previous segment with a small pause -->
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms"" />
                                        <mstts:silence type=""Leading"" value=""0ms"" />
                                        <break time=""250ms"" />
                                        <prosody rate=""1.1665"">यह खंड 4 है</prosody>
                                        <!-- Warning: Intervention Requied - After translation this segment overlapped with previous segment, also the pause for this segment in the source was relatively large (and yet the translated segment did not fit), hence this segment was sped up and placed after the previous segment with a small pause -->
                                      </voice>
                                    </speak>";

            expectedOutput = XDocument.Parse(expectedOutput.Replace("\n", "")).ToString();

            //create input
            List<PreProcessTTSInput> testInput = new List<PreProcessTTSInput>();

            PreProcessTTSInput segment1 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 1 है",
                LexicalText = "This is segment 1",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 1,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 0),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment1.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorMiddle;
            segment1.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment1.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment1.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment1);

            PreProcessTTSInput segment2 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 2 है",
                LexicalText = "This is segment 2",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 2,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 2),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment2.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorMiddle;
            segment2.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment2.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment2.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment2);

            PreProcessTTSInput segment3 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 3 है",
                LexicalText = "This is segment 3",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 3,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 4),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment3.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorMiddle;
            segment3.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment3.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment3.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment3);

            PreProcessTTSInput segment4 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 4 है",
                LexicalText = "This is segment 4",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 4,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 6),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment4.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorMiddle;
            segment4.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment4.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment4.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment4);

            // run test

            var actualOutput = XDocument.Parse(preProcessTTS.DoTTSPreProcessing(testInput)).ToString();

            Assert.AreEqual(expectedOutput.Replace("\n", ""), actualOutput.Replace("\n", ""));
        }

        [TestMethod]
        public void CompensatePausesAnchorStart_TargetFitsInTimeSpan()
        {
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();

            var durationDictionary = new Dictionary<string, int>
            {
                { "यह खंड 1 है", 1500 },
                { "यह खंड 2 है", 1500 },
                { "यह खंड 3 है", 1500 },
                { "यह खंड 4 है", 1500 }
            };

            var preProcessTTS = new PreProcessTTS(loggerMock.Object, new MockSpeechSynthesizer(durationDictionary));

            var expectedOutput = $@"<?xml version=""1.0""?>
                                    <speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xmlns:mstts=""https://www.w3.org/2001/mstts"" xml:lang=""en-US"">
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""0ms""/>
                                        <prosody rate=""{defaultSpeechRate}"">यह खंड 1 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""500ms""/>
                                        <prosody rate=""{defaultSpeechRate}"">यह खंड 2 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""500ms""/>
                                        <prosody rate=""{defaultSpeechRate}"">यह खंड 3 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms""/>
                                        <mstts:silence type=""Leading"" value=""0ms""/>
                                        <break time=""500ms""/>
                                        <prosody rate=""{defaultSpeechRate}"">यह खंड 4 है</prosody>
                                      </voice>
                                    </speak>";

            expectedOutput = XDocument.Parse(expectedOutput.Replace("\n", "")).ToString();

            //create input
            List<PreProcessTTSInput> testInput = new List<PreProcessTTSInput>();

            PreProcessTTSInput segment1 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 1 है",
                LexicalText = "This is segment 1",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 1,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 0),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment1.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorStart;
            segment1.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment1.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment1.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment1);

            PreProcessTTSInput segment2 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 2 है",
                LexicalText = "This is segment 2",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 2,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 2),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment2.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorStart;
            segment2.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment2.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment2.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment2);

            PreProcessTTSInput segment3 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 3 है",
                LexicalText = "This is segment 3",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 3,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 4),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment3.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorStart;
            segment3.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment3.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment3.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment3);

            PreProcessTTSInput segment4 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 4 है",
                LexicalText = "This is segment 4",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 4,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 6),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment4.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorStart;
            segment4.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment4.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment4.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment4);

            // run test

            var actualOutput = XDocument.Parse(preProcessTTS.DoTTSPreProcessing(testInput)).ToString();

            Assert.AreEqual(expectedOutput.Replace("\n", ""), actualOutput.Replace("\n", ""));
        }

        [TestMethod]
        public void CompensatePausesAnchorStart_TargetDoesNotFitsInTimeSpanSpeedUp()
        {
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();

            var durationDictionary = new Dictionary<string, int>
            {
                { "यह खंड 1 है", 2000 },
                { "यह खंड 2 है", 2000 },
                { "यह खंड 3 है", 2000 },
                { "यह खंड 4 है", 2000 }
            };

            var preProcessTTS = new PreProcessTTS(loggerMock.Object, new MockSpeechSynthesizer(durationDictionary));

            var expectedOutput = @"<speak version=""1.0"" xmlns=""http://www.w3.org/2001/10/synthesis"" xmlns:mstts=""https://www.w3.org/2001/mstts"" xml:lang=""en-US"">
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms"" />
                                        <mstts:silence type=""Leading"" value=""0ms"" />
                                        <break time=""0ms"" />
                                        <prosody rate=""1.1428571428571428"">यह खंड 1 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms"" />
                                        <mstts:silence type=""Leading"" value=""0ms"" />
                                        <break time=""250ms"" />
                                        <prosody rate=""1.1428571428571428"">यह खंड 2 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms"" />
                                        <mstts:silence type=""Leading"" value=""0ms"" />
                                        <break time=""250ms"" />
                                        <prosody rate=""1.1428571428571428"">यह खंड 3 है</prosody>
                                      </voice>
                                      <voice name=""TestVoice"">
                                        <mstts:silence type=""Tailing"" value=""0ms"" />
                                        <mstts:silence type=""Leading"" value=""0ms"" />
                                        <break time=""250ms"" />
                                        <prosody rate=""1"">यह खंड 4 है</prosody>
                                      </voice>
                                    </speak>";

            expectedOutput = XDocument.Parse(expectedOutput.Replace("\n", "")).ToString();

            //create input
            List<PreProcessTTSInput> testInput = new List<PreProcessTTSInput>();

            PreProcessTTSInput segment1 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 1 है",
                LexicalText = "This is segment 1",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 1,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 0),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment1.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorStart;
            segment1.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment1.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment1.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment1);

            PreProcessTTSInput segment2 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 2 है",
                LexicalText = "This is segment 2",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 2,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 2),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment2.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorStart;
            segment2.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment2.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment2.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment2);

            PreProcessTTSInput segment3 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 3 है",
                LexicalText = "This is segment 3",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 3,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 4),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment3.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorStart;
            segment3.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment3.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment3.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment3);

            PreProcessTTSInput segment4 = new PreProcessTTSInput
            {
                TranslatedText = "यह खंड 4 है",
                LexicalText = "This is segment 4",
                IdentifiedLocale = "en-US",
                TargetLocale = "hi-IN",
                SegmentID = 4,
                Duration = TimeSpan.FromSeconds(1),
                Offset = new TimeSpan(0, 0, 0, 6),
                PreProcessingStepConfig = new TTSPreProcessingConfiguration()
            };
            segment4.PreProcessingStepConfig.TranslationMappingMethod = TranslationMappingMethodEnum.CompensatePauses_AnchorStart;
            segment4.VoiceInfo = new VoiceDetail
            {
                VoiceName = "TestVoice"
            };
            segment4.PreProcessingStepConfig.MaxSpeechRate = 1.5;
            segment4.PreProcessingStepConfig.MinSpeechRate = 0.5;
            testInput.Add(segment4);

            // run test

            var actualOutput = XDocument.Parse(preProcessTTS.DoTTSPreProcessing(testInput)).ToString();

            Assert.AreEqual(expectedOutput.Replace("\n", ""), actualOutput.Replace("\n", ""));
        }
    }

    public class TestUnitSpeechSynthesizer: ISpeechSynthesizer
    {
        public SpeechResult SpeakTextAsync(string text) => new SpeechResult(null, new TimeSpan(0, 0, 1));

        public SpeechResult SpeakSsmlAsync(string SSML) => new SpeechResult(null, new TimeSpan(0, 0, 1));
    }

    public class MockSpeechSynthesizer : ISpeechSynthesizer
    {
        readonly Dictionary<string, int> mockDurations;
        public MockSpeechSynthesizer(Dictionary<string, int> mockDurations)
        {
            this.mockDurations= mockDurations;
        }

        public SpeechResult SpeakTextAsync(string text)
        {
            var result = new SpeechResult(null, new TimeSpan(0, 0, 0, 0, mockDurations[text]));
            return result;
        }

        public SpeechResult SpeakSsmlAsync(string SSML)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(SSML);
            double rate = Double.Parse(doc.DocumentElement?.ChildNodes[0]["prosody"].Attributes?["rate"].Value);

            var result = new SpeechResult(null, new TimeSpan(0, 0, 0, 0,(int) ((double) mockDurations[doc.InnerText]/rate)));
            return result;
        }
    }
}
