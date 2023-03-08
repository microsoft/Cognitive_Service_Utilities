//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
using AIPlatform.TestingFramework;
using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Evaluation.Translation;
using AIPlatform.TestingFramework.STT;
using AIPlatform.TestingFramework.STT.TranscriptionUtils;
using AIPlatform.TestingFramework.Translation;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace AIPlatform.EvaluationFramework.Test.Unit
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class TranslationCorrectnessEvaluationTest
    {
        private SpeechOutputSegment TranscriptionTestSegment;
        private TranslatorOutputSegment TranslationTestSegment;

        [TestInitialize]
        public void Initialize()
        {
            TranscriptionTestSegment = new SpeechOutputSegment
            {
                SegmentID = 0,
                DisplayText = "It's time to eat.",
                LexicalText = "its time to eat",
                Offset = new TimeSpan(5),
                Duration = new TimeSpan(25),
                IdentifiedLocale = "en-US",
                TimeStamps = new List<TimeStamp>()
                {
                    new TimeStamp("its", new TimeSpan(5), new TimeSpan(5)),
                    new TimeStamp("time", new TimeSpan(5), new TimeSpan(10)),
                    new TimeStamp("to", new TimeSpan(5), new TimeSpan(15)),
                    new TimeStamp("eat", new TimeSpan(5), new TimeSpan(20))
                },

                DisplayWordTimeStamps = new List<TimeStamp>()
                {
                    new TimeStamp("It's", new TimeSpan(5), new TimeSpan(5)),
                    new TimeStamp("time", new TimeSpan(5), new TimeSpan(10)),
                    new TimeStamp("to", new TimeSpan(5), new TimeSpan(15)),
                    new TimeStamp("eat", new TimeSpan(5), new TimeSpan(20))
                },

                NBest = new List<SpeechCandidate>
                {
                    new SpeechCandidate()
                    {
                        LexicalText = "its time to eat",
                        Confidence = 92,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("its", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("time", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("to", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("eat", new TimeSpan(5), new TimeSpan(20))
                        }
                    },
                    new SpeechCandidate()
                    {
                        LexicalText = "it is time to eat",
                        Confidence = 94,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("it", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("time", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("to", new TimeSpan(5), new TimeSpan(20)),
                            new TimeStamp("eat", new TimeSpan(5), new TimeSpan(25))
                        }
                    },
                    new SpeechCandidate()
                    {
                        LexicalText = "its time to it",
                        Confidence = 90,
                        Words = new TimeStamp[]
                        {
                            new TimeStamp("its", new TimeSpan(5), new TimeSpan(5)),
                            new TimeStamp("time", new TimeSpan(5), new TimeSpan(10)),
                            new TimeStamp("to", new TimeSpan(5), new TimeSpan(15)),
                            new TimeStamp("it", new TimeSpan(5), new TimeSpan(20))
                        }
                    }
                }
            };
            TranslationTestSegment = new TranslatorOutputSegment("Es hora de comer", "en-US", "es-MX", 0);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TranslationCorrectnessEval_ThrowsArgumentNullException_If_Threshold_OutOfBoundsAsync()
        {
            //Setup
            var translatorMock = new Mock<ITranslator>();
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var translationCorrectnessEvaluator = new TranslationCorrectnessEvaluator(translatorMock.Object, storageManagerMock.Object, loggerMock.Object);

            var config = new TranslationCorrectnessConfiguration
            {
                Threshold = 2.0
            };

            var transcriptionInput = new List<SpeechOutputSegment>()
            {
                TranscriptionTestSegment
            };

            var translationInput = new List<TranslatorOutputSegment>()
            {
                TranslationTestSegment
            };

            //Execute Test
            await translationCorrectnessEvaluator.EvaluateCorrectnessAsync(new TranslationCorrectnessInput(config, transcriptionInput, translationInput));
        }

        /// <summary>
        /// This test will ensure that when the threshold is 0, a translation which is exactly the same as the bi-directional 
        /// translation will not be flagged for review.
        /// </summary>
        [TestMethod]
        public async Task TranslationCorrectnessEval_PerfectTranslation_IsNotFlaggedForReview_When_Threshold_IsZero()
        {
            //Setup
            var retranslatedResult = new List<TranslatorOutputSegment>
            {
                new TranslatorOutputSegment("es hora de comer", "en-US", "es-MX", 0)
            };
            var translatorMock = new Mock<ITranslator>();
            translatorMock
                .Setup((mock) => mock.DoTranslation(It.IsAny<TranslatorInput>()))
                .ReturnsAsync(JsonConvert.SerializeObject(retranslatedResult));
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var translationCorrectnessEvaluator = new TranslationCorrectnessEvaluator(translatorMock.Object, storageManagerMock.Object, loggerMock.Object);

            var config = new TranslationCorrectnessConfiguration
            {
                Threshold = 0.0
            };

            var transcriptionInput = new List<SpeechOutputSegment>()
            {
                TranscriptionTestSegment
            };

            var translationInput = new List<TranslatorOutputSegment>()
            {
                TranslationTestSegment
            };

            //Execute Test
            var result = JsonConvert.DeserializeObject<List<TranslationCorrectnessOutputSegment>>(await translationCorrectnessEvaluator.EvaluateCorrectnessAsync(new TranslationCorrectnessInput(config, transcriptionInput, translationInput)));

            // Verify result
            var expectedResult = new List<TranslationCorrectnessOutputSegment>
            {
                new TranslationCorrectnessOutputSegment()
                {
                    SegmentID = 0,
                    InterventionNeeded = false
                }
            };

            Assert.IsTrue(expectedResult.SequenceEqual(result, new SegmentEqualityComparer()));
        }

        /// <summary>
        /// This test will ensure that when the threshold is 0, a translation which is not 100% bi-directional will be flagged for review.
        /// </summary>
        [TestMethod]
        public async Task TranslationCorrectnessEval_ImperfectTranslation_IsFlaggedForReview_When_Threshold_IsZero()
        {
            //Setup
            var retranslatedResult = new List<TranslatorOutputSegment>
            {
                new TranslatorOutputSegment("es la hora de comer", "en-US", "es-MX", 0)
            };
            var translatorMock = new Mock<ITranslator>();
            translatorMock
                .Setup((mock) => mock.DoTranslation(It.IsAny<TranslatorInput>()))
                .ReturnsAsync(JsonConvert.SerializeObject(retranslatedResult));
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var translationCorrectnessEvaluator = new TranslationCorrectnessEvaluator(translatorMock.Object, storageManagerMock.Object, loggerMock.Object);

            var config = new TranslationCorrectnessConfiguration
            {
                Threshold = 0.0
            };

            var transcriptionInput = new List<SpeechOutputSegment>()
            {
                TranscriptionTestSegment
            };

            var translationInput = new List<TranslatorOutputSegment>()
            {
                TranslationTestSegment
            };

            //Execute Test
            var result = JsonConvert.DeserializeObject<List<TranslationCorrectnessOutputSegment>>(await translationCorrectnessEvaluator.EvaluateCorrectnessAsync(new TranslationCorrectnessInput(config, transcriptionInput, translationInput)));

            // Verify result
            var expectedResult = new List<TranslationCorrectnessOutputSegment>
            {
                new TranslationCorrectnessOutputSegment()
                {
                    SegmentID = 0,
                    InterventionNeeded = true
                }
            };

            Assert.IsTrue(expectedResult.SequenceEqual(result, new SegmentEqualityComparer()));
        }

        /// <summary>
        /// This test will ensure that when the threshold is 1.0, a translation which is slightly different from the bi-directional translation
        /// but not 100% different from it will not be flagged for review.
        /// </summary>
        [TestMethod]
        public async Task TranslationCorrectnessEval_Translation_IsNotFlaggedForReview_When_BidirectionalTranslationDifference_IsWithinThreshold()
        {
            //Setup
            var retranslatedResult = new List<TranslatorOutputSegment>
            {
                new TranslatorOutputSegment("es la hora de comer", "en-US", "es-MX", 0)
            };
            var translatorMock = new Mock<ITranslator>();
            translatorMock
                .Setup((mock) => mock.DoTranslation(It.IsAny<TranslatorInput>()))
                .ReturnsAsync(JsonConvert.SerializeObject(retranslatedResult));
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var storageManagerMock = new Mock<IStorageManager>();
            var translationCorrectnessEvaluator = new TranslationCorrectnessEvaluator(translatorMock.Object, storageManagerMock.Object, loggerMock.Object);

            var config = new TranslationCorrectnessConfiguration
            {
                Threshold = 1.0
            };

            var transcriptionInput = new List<SpeechOutputSegment>()
            {
                TranscriptionTestSegment
            };

            var translationInput = new List<TranslatorOutputSegment>()
            {
                TranslationTestSegment
            };

            //Execute Test
            var result = JsonConvert.DeserializeObject<List<TranslationCorrectnessOutputSegment>>(await translationCorrectnessEvaluator.EvaluateCorrectnessAsync(new TranslationCorrectnessInput(config, transcriptionInput, translationInput)));

            // Verify result
            var expectedResult = new List<TranslationCorrectnessOutputSegment>
            {
                new TranslationCorrectnessOutputSegment()
                {
                    SegmentID = 0,
                    InterventionNeeded = false
                }
            };

            Assert.IsTrue(expectedResult.SequenceEqual(result, new SegmentEqualityComparer()));
        }

        private class SegmentEqualityComparer : IEqualityComparer<TranslationCorrectnessOutputSegment>
        {
            public bool Equals(TranslationCorrectnessOutputSegment segment1, TranslationCorrectnessOutputSegment segment2)
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

                if (segment1.SegmentID == segment2.SegmentID && segment1.InterventionNeeded == segment2.InterventionNeeded)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            public int GetHashCode(TranslationCorrectnessOutputSegment segment)
            {
                if (ReferenceEquals(segment, null))
                {
                    return 0;
                }

                var hash = 17 * segment.SegmentID.GetHashCode() + segment.InterventionNeeded.GetHashCode();

                return hash;
            }
        }
    }
}
