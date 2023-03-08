//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework;
using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Evaluation.Interfaces;
using AIPlatform.TestingFramework.Evaluation.STT;
using AIPlatform.TestingFramework.STT;
using AIPlatform.TestingFramework.STT.TranscriptionUtils;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AIPlatform.EvaluationFramework.Test.Unit
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class SpeechCorrectnessEvaluationTest
    {
        private SpeechOutputSegment TestSegment;

        [TestInitialize]
        public void Initialize()
        {
            TestSegment = new SpeechOutputSegment
            {
                SegmentID = 1,
                DisplayText = "This is a speech correctness evaluation nest.",
                LexicalText = "this is a speech correctness evaluation nest",
                Offset = new TimeSpan(5),
                Duration = new TimeSpan(40),
                IdentifiedLocale = "en-US",
                TimeStamps = new List<TimeStamp>()
                {
                    new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                    new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                    new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                    new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                    new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                    new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                    new TimeStamp("nest", new TimeSpan(5), new TimeSpan(35))
                },

                DisplayWordTimeStamps = new List<TimeStamp>()
                {
                    new TimeStamp("This", new TimeSpan(5), new TimeSpan(5)),
                    new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                    new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                    new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                    new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                    new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                    new TimeStamp("nest.", new TimeSpan(5), new TimeSpan(35))
                },

                NBest = new List<SpeechCandidate>
                {
                    new SpeechCandidate()
                    {
                        LexicalText = "this is a speech correctness evaluation nest",
                        Confidence = 92,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                            new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                            new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                            new TimeStamp("nest", new TimeSpan(5), new TimeSpan(35))
                        }
                    },
                    new SpeechCandidate()
                    {
                        LexicalText = "this is a speech correctness evaluation test",
                        Confidence = 94,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                            new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                            new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                            new TimeStamp("test", new TimeSpan(5), new TimeSpan(35))
                        }
                    },
                    new SpeechCandidate()
                    {
                        LexicalText = "this as a speech correctness evaluation test",
                        Confidence = 93,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("as", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                            new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                            new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                            new TimeStamp("test", new TimeSpan(5), new TimeSpan(35))
                        }
                    },
                    new SpeechCandidate()
                    {
                        LexicalText = "this this as a speech correctness evaluation test",
                        Confidence = 89,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("this", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("as", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("a", new TimeSpan(5), new TimeSpan(20)),
                            new TimeStamp("speech", new TimeSpan(5), new TimeSpan(25)),
                            new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(30)),
                            new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(35)),
                            new TimeStamp("test", new TimeSpan(5), new TimeSpan(40))
                        }
                    },
                    new SpeechCandidate()
                    {
                        LexicalText = "this is a speech correctness test",
                        Confidence = 88,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                            new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                            new TimeStamp("test", new TimeSpan(5), new TimeSpan(30))
                        }
                    },
                }
            };
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SpeechCorrectnessEval_ThrowsArgumentNullException_If_ConfidenceThreshold_OutOfBounds()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var speechCorrectnessEvaluator = new SpeechCorrectnessEvaluator(storageManagerMock.Object, loggerMock.Object);

            SpeechCorrectnessConfiguration config = new SpeechCorrectnessConfiguration
            {
                ConfidenceThreshold = 2.0,
                OccurrenceThreshold = 0.0
            };

            var input = new List<SpeechOutputSegment>()
            {
                TestSegment
            };

            //Execute Test
            speechCorrectnessEvaluator.EvaluateCorrectness(new SpeechCorrectnessInput(config, input));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SpeechCorrectnessEval_ThrowsArgumentNullException_If_OccurrenceThreshold_OutOfBounds()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var speechCorrectnessEvaluator = new SpeechCorrectnessEvaluator(storageManagerMock.Object, loggerMock.Object);

            SpeechCorrectnessConfiguration config = new SpeechCorrectnessConfiguration
            {
                ConfidenceThreshold = 0.0,
                OccurrenceThreshold = 2.0
            };

            var input = new List<SpeechOutputSegment>()
            {
                TestSegment
            };

            //Execute Test
            speechCorrectnessEvaluator.EvaluateCorrectness(new SpeechCorrectnessInput(config, input));
        }

        /// <summary>
        /// This test will ensure that all points of contention will be flagged for review.
        /// </summary>
        [TestMethod]
        public void SpeechCorrectnessEval_ReturnsAllCorrections_If_OccurrenceThreshold_IsZero_And_ConfidenceThreshold_IsOne()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var speechCorrectnessEvaluator = new SpeechCorrectnessEvaluator(storageManagerMock.Object, loggerMock.Object);

            SpeechCorrectnessConfiguration config = new SpeechCorrectnessConfiguration
            {
                ConfidenceThreshold = 1.0,
                OccurrenceThreshold = 0.0
            };

            var input = new List<SpeechOutputSegment>()
            {
                TestSegment
            };

            //Execute Test
            var result = JsonConvert.DeserializeObject<List<SpeechCorrectnessOutputSegment>>(speechCorrectnessEvaluator.EvaluateCorrectness(new SpeechCorrectnessInput(config, input)));

            // Verify result
            var expectedResult = new List<SpeechCorrectnessOutputSegment>
            {
                new SpeechCorrectnessOutputSegment
                {
                    SegmentID = 1,
                    InterventionNeeded = true,
                    InterventionReasons = new List<SpeechPointOfContention>
                    {
                        new SpeechPointOfContention("as", 1, ContentionType.WrongWord),
                        new SpeechPointOfContention("this", 0, ContentionType.MissingWord),
                        new SpeechPointOfContention("test", 6, ContentionType.WrongWord),
                        new SpeechPointOfContention("evaluation", 5, ContentionType.InsertedWord)
                    }
                }
            };

            Assert.IsTrue(expectedResult.SequenceEqual(result, new SegmentEqualityComparer()));
        }

        /// <summary>
        /// This test will ensure that only points of contention that occur in all candidates will be flagged for review.
        /// </summary>
        [TestMethod]
        public void SpeechCorrectnessEval_ReturnsOnlyCorrectionsForUniversalErrors_If_OccurrenceThreshold_IsOne_And_ConfidenceThreshold_IsOne()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var speechCorrectnessEvaluator = new SpeechCorrectnessEvaluator(storageManagerMock.Object, loggerMock.Object);

            SpeechCorrectnessConfiguration config = new SpeechCorrectnessConfiguration
            {
                ConfidenceThreshold = 1.0,
                OccurrenceThreshold = 1.0
            };

            var input = new List<SpeechOutputSegment>()
            {
                TestSegment
            };

            //Execute Test
            var result = JsonConvert.DeserializeObject<List<SpeechCorrectnessOutputSegment>>(speechCorrectnessEvaluator.EvaluateCorrectness(new SpeechCorrectnessInput(config, input)));

            // Verify result
            var expectedResult = new List<SpeechCorrectnessOutputSegment>
            {
                new SpeechCorrectnessOutputSegment
                {
                    SegmentID = 1,
                    InterventionNeeded = true,
                    InterventionReasons = new List<SpeechPointOfContention>
                    {
                        new SpeechPointOfContention("test", 6, ContentionType.WrongWord)
                    }
                }
            };

            Assert.IsTrue(expectedResult.SequenceEqual(result, new SegmentEqualityComparer()));
        }

        /// <summary>
        /// This test will ensure that all points of contention that occur in any candidates that have equal or higher confidence than the selected text will be flagged for review.
        /// </summary>
        [TestMethod]
        public void SpeechCorrectnessEval_ReturnsOnlyCorrectionsFromHigherConfidenceCandidates_If_OccurrenceThreshold_IsZero_And_ConfidenceThreshold_IsZero()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var speechCorrectnessEvaluator = new SpeechCorrectnessEvaluator(storageManagerMock.Object, loggerMock.Object);

            SpeechCorrectnessConfiguration config = new SpeechCorrectnessConfiguration
            {
                ConfidenceThreshold = 0.0,
                OccurrenceThreshold = 0.0
            };

            var input = new List<SpeechOutputSegment>()
            {
                TestSegment
            };

            //Execute Test
            var result = JsonConvert.DeserializeObject<List<SpeechCorrectnessOutputSegment>>(speechCorrectnessEvaluator.EvaluateCorrectness(new SpeechCorrectnessInput(config, input)));

            // Verify result
            var expectedResult = new List<SpeechCorrectnessOutputSegment>
            {
                new SpeechCorrectnessOutputSegment
                {
                    SegmentID = 1,
                    InterventionNeeded = true,
                    InterventionReasons = new List<SpeechPointOfContention>
                    {
                        new SpeechPointOfContention("as", 1, ContentionType.WrongWord),
                        new SpeechPointOfContention("test", 6, ContentionType.WrongWord)
                    }
                }
            };

            Assert.IsTrue(expectedResult.SequenceEqual(result, new SegmentEqualityComparer()));
        }

        /// <summary>
        /// This test will ensure that only points of contention that occur in all candidates that have equal or higher confidence than the selected text will be flagged for review.
        /// </summary>
        [TestMethod]
        public void SpeechCorrectnessEval_ReturnsOnlyCorrectionsFromHigherConfidenceCandidates_If_OccurrenceThreshold_IsOne_And_ConfidenceThreshold_IsZero()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var speechCorrectnessEvaluator = new SpeechCorrectnessEvaluator(storageManagerMock.Object, loggerMock.Object);

            SpeechCorrectnessConfiguration config = new SpeechCorrectnessConfiguration
            {
                ConfidenceThreshold = 0.0,
                OccurrenceThreshold = 1.0
            };

            var input = new List<SpeechOutputSegment>()
            {
                TestSegment
            };

            //Execute Test
            var result = JsonConvert.DeserializeObject<List<SpeechCorrectnessOutputSegment>>(speechCorrectnessEvaluator.EvaluateCorrectness(new SpeechCorrectnessInput(config, input)));

            // Verify result
            var expectedResult = new List<SpeechCorrectnessOutputSegment>
            {
                new SpeechCorrectnessOutputSegment
                {
                    SegmentID = 1,
                    InterventionNeeded = true,
                    InterventionReasons = new List<SpeechPointOfContention>
                    {
                        new SpeechPointOfContention("test", 6, ContentionType.WrongWord)
                    }
                }
            };

            Assert.IsTrue(expectedResult.SequenceEqual(result, new SegmentEqualityComparer()));
        }

        /// <summary>
        /// This test will ensure that when multiple the candidates are the same, that they will still be counted as their own candidates to be used for comparison.
        /// </summary>
        [TestMethod]
        public void SpeechCorrectnessEval_ReturnsExpectedResult_When_CandidatesAreTheSame()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var speechCorrectnessEvaluator = new SpeechCorrectnessEvaluator(storageManagerMock.Object, loggerMock.Object);

            SpeechCorrectnessConfiguration config = new SpeechCorrectnessConfiguration
            {
                ConfidenceThreshold = 1.0,
                OccurrenceThreshold = 0.5
            };

            var testSegment = new SpeechOutputSegment
            {
                SegmentID = 1,
                DisplayText = "This is a speech correctness evaluation nest.",
                LexicalText = "this is a speech correctness evaluation nest",
                Offset = new TimeSpan(5),
                Duration = new TimeSpan(40),
                IdentifiedLocale = "en-US",
                TimeStamps = new List<TimeStamp>()
                {
                    new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                    new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                    new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                    new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                    new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                    new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                    new TimeStamp("nest", new TimeSpan(5), new TimeSpan(35))
                },

                DisplayWordTimeStamps = new List<TimeStamp>()
                {
                    new TimeStamp("This", new TimeSpan(5), new TimeSpan(5)),
                    new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                    new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                    new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                    new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                    new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                    new TimeStamp("nest.", new TimeSpan(5), new TimeSpan(35))
                },

                NBest = new List<SpeechCandidate>
                {
                    new SpeechCandidate()
                    {
                        LexicalText = "this is a speech correctness evaluation nest",
                        Confidence = 92,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                            new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                            new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                            new TimeStamp("nest", new TimeSpan(5), new TimeSpan(35))
                        }
                    },
                    new SpeechCandidate()
                    {
                        LexicalText = "this is a speech correctness evaluation nest",
                        Confidence = 92,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                            new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                            new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                            new TimeStamp("nest", new TimeSpan(5), new TimeSpan(35))
                        }
                    },
                    new SpeechCandidate()
                    {
                        LexicalText = "this is a speech correctness evaluation nest",
                        Confidence = 92,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                            new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                            new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                            new TimeStamp("nest", new TimeSpan(5), new TimeSpan(35))
                        }
                    },
                    new SpeechCandidate()
                    {
                        LexicalText = "this is a speech correctness evaluation nest",
                        Confidence = 92,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                            new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                            new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                            new TimeStamp("nest", new TimeSpan(5), new TimeSpan(35))
                        }
                    },
                    new SpeechCandidate()
                    {
                        LexicalText = "this is a speech correctness evaluation test",
                        Confidence = 91,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                            new TimeStamp("correctness", new TimeSpan(5), new TimeSpan(25)),
                            new TimeStamp("evaluation", new TimeSpan(5), new TimeSpan(30)),
                            new TimeStamp("test", new TimeSpan(5), new TimeSpan(35))
                        }
                    }
                }
            };

            var input = new List<SpeechOutputSegment>()
            {
                testSegment
            };

            //Execute Test
            var result = JsonConvert.DeserializeObject<List<SpeechCorrectnessOutputSegment>>(speechCorrectnessEvaluator.EvaluateCorrectness(new SpeechCorrectnessInput(config, input)));

            // Verify result
            var expectedResult = new List<SpeechCorrectnessOutputSegment>
            {
                new SpeechCorrectnessOutputSegment
                {
                    SegmentID = 1,
                    InterventionNeeded = false,
                    InterventionReasons = null
                }
            };

            Assert.IsTrue(expectedResult.SequenceEqual(result, new SegmentEqualityComparer()));
        }

        private class SegmentEqualityComparer : IEqualityComparer<SpeechCorrectnessOutputSegment>
        {
            public bool Equals(SpeechCorrectnessOutputSegment segment1, SpeechCorrectnessOutputSegment segment2)
            {
                if (ReferenceEquals(segment1, segment2))
                {
                    return true;
                }

                if (ReferenceEquals(null, null))
                {
                    return true;
                }

                if (ReferenceEquals(segment1, null) || ReferenceEquals(segment2, null))
                {
                    return false;
                }

                if (segment1.SegmentID == segment2.SegmentID && segment1.InterventionNeeded == segment2.InterventionNeeded && segment1.InterventionReasons.ToHashSet().SetEquals(segment2.InterventionReasons.ToHashSet()))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public int GetHashCode(SpeechCorrectnessOutputSegment segment)
            {
                if (ReferenceEquals(segment, null))
                {
                    return 0;
                }

                var hash = 17 * segment.SegmentID.GetHashCode() + segment.InterventionNeeded.GetHashCode();

                foreach (var interventionReason in segment.InterventionReasons)
                {
                    hash *= interventionReason.GetHashCode();
                }

                return hash;
            }
        }
    }
}
