using AIPlatform.TestingFramework;
using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Translation;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AIPlatform.EvaluationFramework.Test.Unit
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class TranslatorTest
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Translator_ThrowsArgumentNullException_If_Region_Is_NullOrEmpty()
        {
            // Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Execute Test
            var translator = new Translator(loggerMock.Object, httpClientFactoryMock.Object);
            var serviceConfiguration = new CognitiveServiceConfiguration()
            {
                Region = string.Empty,
                SubscriptionKey = "subscriptionkey",
            };

            var config = new TranslatorConfiguration()
            {
                ServiceConfiguration = serviceConfiguration,
                Endpoint = "https://test.com",
                Route = "/translate?api-version=3.0&from=es&to=en",
                IsInputSegmented = false
            };
            var inputSegment = new TranslatorInputSegment("Es hora de comer.");
            var inputSegments = new List<TranslatorInputSegment>() { inputSegment };

            await translator.DoTranslation(new TranslatorInput(config, inputSegments));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Translator_ThrowsArgumentNullException_If_Subscription_Is_NullOrEmpty()
        {
            // Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Execute Test
            var translator = new Translator(loggerMock.Object, httpClientFactoryMock.Object);
            var serviceConfiguration = new CognitiveServiceConfiguration()
            {
                Region = "subscriptionkey",
                SubscriptionKey = string.Empty,
            };

            var config = new TranslatorConfiguration()
            {
                ServiceConfiguration = serviceConfiguration,
                Endpoint = "https://test.com",
                Route = "/translate?api-version=3.0&from=es&to=en",
                IsInputSegmented = false
            };
            var inputSegment = new TranslatorInputSegment("Es hora de comer.");
            var inputSegments = new List<TranslatorInputSegment>() { inputSegment };

            await translator.DoTranslation(new TranslatorInput(config, inputSegments));

        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Translator_ThrowsArgumentNullException_If_Endpoint_Is_NullOrEmpty()
        {
            // Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Execute Test
            var translator = new Translator(loggerMock.Object, httpClientFactoryMock.Object);
            var serviceConfiguration = new CognitiveServiceConfiguration()
            {
                Region = "region",
                SubscriptionKey = "subscriptionkey",

            };
            var config = new TranslatorConfiguration()
            {
                ServiceConfiguration= serviceConfiguration,
                Endpoint = string.Empty,
                Route = "/translate?api-version=3.0&from=es&to=en",
                IsInputSegmented = false
            };
            var inputSegment = new TranslatorInputSegment("Es hora de comer.");
            var inputSegments = new List<TranslatorInputSegment>() { inputSegment };

            await translator.DoTranslation(new TranslatorInput(config, inputSegments));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Translator_ThrowsArgumentNullException_If_Route_Is_NullOrEmpty()
        {
            // Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Execute Test
            var translator = new Translator(loggerMock.Object, httpClientFactoryMock.Object);
            var serviceConfiguration = new CognitiveServiceConfiguration()
            {
                Region = "region",
                SubscriptionKey = "subscriptionkey",

            };
            var config = new TranslatorConfiguration()
            {
                ServiceConfiguration = serviceConfiguration,
                Endpoint = "https://test.com",
                Route = string.Empty,
                IsInputSegmented = false
            };
            
            var inputSegment = new TranslatorInputSegment("Es hora de comer.");
            var inputSegments = new List<TranslatorInputSegment>() { inputSegment };

            await translator.DoTranslation(new TranslatorInput(config, inputSegments));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Translator_ThrowsArgumentNullException_If_Input_Is_Null()
        {
            // Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Execute Test
            var translator = new Translator(loggerMock.Object, httpClientFactoryMock.Object);
            var serviceConfiguration = new CognitiveServiceConfiguration()
            {
                Region = "region",
                SubscriptionKey = "subscriptionkey",
            };

            var config = new TranslatorConfiguration()
            {
                ServiceConfiguration = serviceConfiguration,
                Endpoint = "https://test.com",
                Route = "/translate?api-version=3.0&from=es&to=en",
                IsInputSegmented = false
            };

            await translator.DoTranslation(new TranslatorInput(config, null));
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task Translator_ThrowsArgumentNullException_If_Multiple_TargetLocales()
        {
            // Setup
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();

            // Execute Test
            var translator = new Translator(loggerMock.Object, httpClientFactoryMock.Object);
            var serviceConfiguration = new CognitiveServiceConfiguration()
            {
                Region = "region",
                SubscriptionKey = "subscriptionkey",
            };
            var config = new TranslatorConfiguration()
            {
                ServiceConfiguration= serviceConfiguration,
                Endpoint = "https://test.com",
                Route = "/translate?api-version=3.0&from=es&to=en&to=ht",
                IsInputSegmented = false
            };

            var inputSegment = new TranslatorInputSegment("Es hora de comer.");
            var inputSegments = new List<TranslatorInputSegment>() { inputSegment };

            await translator.DoTranslation(new TranslatorInput(config, inputSegments));
        }

        [TestMethod]
        public async Task Translator_SingleInput()
        {
            // Setup
            var text = "Es hora de comer.";
            var expectedTranslation = "It's time to eat.";
            var sourceLocale = "es";
            var targetLocale = "en";
            var expectedSerializedTranslation = $"[{{\"Translations\":[{{\"To\":\"en\",\"Text\":\"It's time to eat.\"}}]}}]";

            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(httpRequestMessage =>
                        JsonConvert.DeserializeObject<TranslationRequestBody[]>(
                            httpRequestMessage.Content.ReadAsStringAsync().Result).First().Text == text &&
                            httpRequestMessage.RequestUri.ParseQueryString()["to"] == targetLocale &&
                            httpRequestMessage.RequestUri.ParseQueryString()["from"] == sourceLocale),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedSerializedTranslation)
                });
            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock
                .Setup(mock => mock.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // Execute Test
            var translator = new Translator(loggerMock.Object, httpClientFactoryMock.Object);
            var serviceConfiguration = new CognitiveServiceConfiguration()
            {
                Region = "region",
                SubscriptionKey = "subscriptionkey",
            };
            var config = new TranslatorConfiguration()
            {
                ServiceConfiguration = serviceConfiguration,
                Endpoint = "https://test.com",
                Route = $"/translate?api-version=3.0&from={sourceLocale}&to={targetLocale}",
                IsInputSegmented = false
            };

            var inputSegment = new TranslatorInputSegment(text);
            var inputSegments = new List<TranslatorInputSegment>() { inputSegment };

            var translation = await translator.DoTranslation(new TranslatorInput(config, inputSegments));

            // Evaluate result
            Assert.AreEqual(expectedTranslation, translation);
        }

        [TestMethod]
        public async Task Translator_MultipleInput()
        {
            // Setup
            var texts = new string[]
            {
                "Es hora de comer.",
                "È ora di mangiare.",
                "È ora di mangiare."
            };
            var sourceLocales = new string[] { "es", "it", "it" };
            var targetLocales = new string[] { "en", "en", "es" };
            var expectedSerializedTranslations = new string[]
            {
                $"[{{\"Translations\":[{{\"To\":\"en\",\"Text\":\"It's time to eat.\"}}]}}]",
                $"[{{\"Translations\":[{{\"To\":\"en\",\"Text\":\"It's time to eat.\"}}]}}]",
                $"[{{\"Translations\":[{{\"To\":\"es\",\"Text\":\"Es hora de comer.\"}}]}}]"
            };
            var expectedTranslations = new string[]
            {
                "It's time to eat.",
                "It's time to eat.",
                "Es hora de comer."
            };
            var defaultSourceLocale = "es";
            var defaultTargetLocale = "en";            

            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(httpRequestMessage =>
                        JsonConvert.DeserializeObject<TranslationRequestBody[]>(
                            httpRequestMessage.Content.ReadAsStringAsync().Result).First().Text == texts[0] &&
                            httpRequestMessage.RequestUri.ParseQueryString()["to"] == targetLocales[0] &&
                            httpRequestMessage.RequestUri.ParseQueryString()["from"] == sourceLocales[0]),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedSerializedTranslations[0])
                });
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(httpRequestMessage =>
                        JsonConvert.DeserializeObject<TranslationRequestBody[]>(
                            httpRequestMessage.Content.ReadAsStringAsync().Result).First().Text == texts[1] &&
                            httpRequestMessage.RequestUri.ParseQueryString()["to"] == targetLocales[1] &&
                            httpRequestMessage.RequestUri.ParseQueryString()["from"] == sourceLocales[1]),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedSerializedTranslations[1])
                });
            mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(httpRequestMessage =>
                        JsonConvert.DeserializeObject<TranslationRequestBody[]>(
                            httpRequestMessage.Content.ReadAsStringAsync().Result).First().Text == texts[2] &&
                            httpRequestMessage.RequestUri.ParseQueryString()["to"] == targetLocales[2] &&
                            httpRequestMessage.RequestUri.ParseQueryString()["from"] == sourceLocales[2]),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(expectedSerializedTranslations[2])
                });
            var httpClient = new HttpClient(mockHttpMessageHandler.Object);
            var httpClientFactoryMock = new Mock<IHttpClientFactory>();
            httpClientFactoryMock
                .Setup(mock => mock.CreateClient(It.IsAny<string>()))
                .Returns(httpClient);

            // Execute Test
            var translator = new Translator(loggerMock.Object, httpClientFactoryMock.Object);
            var serviceConfiguration = new CognitiveServiceConfiguration()
            {
                Region = "region",
                SubscriptionKey = "subscriptionkey",
            };
            var config = new TranslatorConfiguration()
            {
                ServiceConfiguration = serviceConfiguration,
                Endpoint = "https://test.com",
                Route = $"/translate?api-version=3.0&from={defaultSourceLocale}&to={defaultTargetLocale}",
                IsInputSegmented = true
            };

            var inputSegments = new List<TranslatorInputSegment>()
            {
                new TranslatorInputSegment("Es hora de comer.", null, null, 0),
                new TranslatorInputSegment("È ora di mangiare.", "it", "en", 1),
                new TranslatorInputSegment("È ora di mangiare.", "it", "es", 2)
            };
            var translationsStr = await translator.DoTranslation(new TranslatorInput(config, inputSegments));
            var translations = JsonConvert.DeserializeObject<ICollection<TranslatorOutputSegment>>(translationsStr);

            for (int i = 0; i < translations.Count; i++)
            {
                // Evaluate result
                Assert.AreEqual(expectedTranslations[i], translations.ElementAt(i).TranslatedText);
                Assert.AreEqual(i, translations.ElementAt(i).SegmentID);
            }
        }
    }
}
