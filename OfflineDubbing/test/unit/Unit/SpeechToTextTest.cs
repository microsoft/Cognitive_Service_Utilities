//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework;
using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.STT;
using AIPlatform.TestingFramework.STT.TranscriptionUtils;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace AIPlatform.EvaluationFramework.Test.Unit
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class SpeechToTextTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SpeechToTextService_ThrowsArgumentNullException_If_Region_Is_NullOrEmpty()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var storageManagerMock = new Mock<IStorageManager>();

            //Execute Test
            var speechService = new SpeechToText(loggerMock.Object, httpClientFactoryMock.Object, storageManagerMock.Object);

            CognitiveServiceConfiguration svcConfig = new CognitiveServiceConfiguration
            {
                Region = string.Empty
            };

            SpeechConfiguration config = new SpeechConfiguration
            {
                ServiceConfiguration = svcConfig
            };

            await speechService.DoTranscription(new SpeechInput(config, ""));

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task SpeechToTextService_ThrowsArgumentNullException_If_Subscription_Is_NullOrEmpty()
        {
            //Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            var storageManagerMock = new Mock<IStorageManager>();

            //Execute Test
            var speechService = new SpeechToText(loggerMock.Object, httpClientFactoryMock.Object, storageManagerMock.Object);
            CognitiveServiceConfiguration svcConfig = new CognitiveServiceConfiguration
            {
                SubscriptionKey = string.Empty
            };

            SpeechConfiguration config = new SpeechConfiguration
            {
                ServiceConfiguration = svcConfig
            };

            await speechService.DoTranscription(new SpeechInput(config, ""));

        }

        private void AssertNBestAreEqual(ICollection<SpeechCandidate> expectedNBest, ICollection<SpeechCandidate> actualNBest)
        {
            Assert.AreEqual(expectedNBest.Count, actualNBest.Count);
            for (int i = 0; i < expectedNBest.Count; i++)
            {
                var expectedCandidate = expectedNBest.ElementAt(i);
                var actualCandidate = actualNBest.ElementAt(i);
                Assert.AreEqual(expectedCandidate.LexicalText, actualCandidate.LexicalText);
                Assert.AreEqual(expectedCandidate.Confidence, actualCandidate.Confidence);

                var expectedWords = expectedCandidate.Words;
                var actualWords = actualCandidate.Words;
                Assert.AreEqual(expectedWords.Length, actualWords.Length);

                for (int j = 0; j < expectedWords.Length; j++)
                {
                    Assert.AreEqual(expectedWords[j].Word, actualWords[j].Word);
                    Assert.AreEqual(expectedWords[j].Duration, actualWords[j].Duration);
                    Assert.AreEqual(expectedWords[j].Offset, actualWords[j].Offset);
                }
            }
        }

        [TestMethod]
        public void SpeechToText_SpeechOutputSegmentSplitTest()
        {
            // Setup
            SpeechOutputSegment testSegment = new SpeechOutputSegment()
            {
                DisplayText = "This is a speech segment split test.",
                LexicalText = "this is a speech segment split test",
                Offset = new TimeSpan(5),
                Duration = new TimeSpan(40),
                IdentifiedLocale = "en-US"
            };

            testSegment.TimeStamps = new List<TimeStamp>()
            {
                new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                new TimeStamp("segment", new TimeSpan(5), new TimeSpan(25)),
                new TimeStamp("split", new TimeSpan(5), new TimeSpan(30)),
                new TimeStamp("test", new TimeSpan(5), new TimeSpan(35))
            };

            testSegment.DisplayWordTimeStamps = new List<TimeStamp>()
            {
                new TimeStamp("This", new TimeSpan(5), new TimeSpan(5)),
                new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                new TimeStamp("segment", new TimeSpan(5), new TimeSpan(25)),
                new TimeStamp("split", new TimeSpan(5), new TimeSpan(30)),
                new TimeStamp("test.", new TimeSpan(5), new TimeSpan(35))
            };

            testSegment.NBest = new List<SpeechCandidate>
            {
                new SpeechCandidate()
                {
                    LexicalText = "this is a speech segment split test",
                    Confidence = 95,
                    Words = new TimeStamp[]
                    {
                        new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                        new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                        new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                        new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                        new TimeStamp("segment", new TimeSpan(5), new TimeSpan(25)),
                        new TimeStamp("split", new TimeSpan(5), new TimeSpan(30)),
                        new TimeStamp("test", new TimeSpan(5), new TimeSpan(35))
                    }
                },
                new SpeechCandidate()
                {
                    LexicalText = "this as a speech segment split nest",
                    Confidence = 93,
                    Words = new TimeStamp[]
                    {
                        new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                        new TimeStamp("as", new TimeSpan(5), new TimeSpan(10)),
                        new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                        new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                        new TimeStamp("segment", new TimeSpan(5), new TimeSpan(25)),
                        new TimeStamp("split", new TimeSpan(5), new TimeSpan(30)),
                        new TimeStamp("nest", new TimeSpan(5), new TimeSpan(35))
                    }
                }
            };

            var expectedFirstSegmentNBest = new List<SpeechCandidate>()
            {
                new SpeechCandidate()
                {
                    LexicalText = "this is a",
                    Confidence = 95,
                    Words = new TimeStamp[]
                    {
                        new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                        new TimeStamp("is", new TimeSpan(5), new TimeSpan(10)),
                        new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                    }
                },
                new SpeechCandidate()
                {
                    LexicalText = "this as a",
                    Confidence = 93,
                    Words = new TimeStamp[]
                    {
                        new TimeStamp("this", new TimeSpan(5), new TimeSpan(5)),
                        new TimeStamp("as", new TimeSpan(5), new TimeSpan(10)),
                        new TimeStamp("a", new TimeSpan(5), new TimeSpan(15)),
                    }
                }
            };


            var expectedSecondSegmentNBest = new List<SpeechCandidate>()
            {
                new SpeechCandidate()
                {
                    LexicalText = "speech segment split test",
                    Confidence = 95,
                    Words = new TimeStamp[]
                    {
                        new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                        new TimeStamp("segment", new TimeSpan(5), new TimeSpan(25)),
                        new TimeStamp("split", new TimeSpan(5), new TimeSpan(30)),
                        new TimeStamp("test", new TimeSpan(5), new TimeSpan(35))
                    }
                },
                new SpeechCandidate()
                {
                    LexicalText = "speech segment split nest",
                    Confidence = 93,
                    Words = new TimeStamp[]
                    {
                        new TimeStamp("speech", new TimeSpan(5), new TimeSpan(20)),
                        new TimeStamp("segment", new TimeSpan(5), new TimeSpan(25)),
                        new TimeStamp("split", new TimeSpan(5), new TimeSpan(30)),
                        new TimeStamp("nest", new TimeSpan(5), new TimeSpan(35))
                    }
                },
            };

            // Execute test
            (SpeechOutputSegment firstSegment, SpeechOutputSegment secondSegment) = testSegment.SplitSegment(new TimeSpan(22));

            // Assert result is expected
            Assert.AreEqual("This is a", firstSegment.DisplayText);
            Assert.AreEqual("speech segment split test.", secondSegment.DisplayText);
            AssertNBestAreEqual(expectedFirstSegmentNBest, firstSegment.NBest);
            AssertNBestAreEqual(expectedSecondSegmentNBest, secondSegment.NBest);
        }
    }
}
