using AIPlatform.TestingFramework.Utilities.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AIPlatform.TestingFramework.Translation
{
    public class Translator : ITranslator
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;
        private readonly IHttpClientFactory httpClientFactory;

        public Translator(IOrchestratorLogger<TestingFrameworkOrchestrator> logger, IHttpClientFactory httpClientFactory)
        {
            this.logger = logger;
            this.httpClientFactory = httpClientFactory;
        }

        public class TranslationResult
        {
            public DetectedLanguage DetectedLanguage { get; set; }
            public TextResult SourceText { get; set; }
            public Translation[] Translations { get; set; }
        }

        public class DetectedLanguage
        {
            public string Language { get; set; }
            public float Score { get; set; }
        }

        public class TextResult
        {
            public string Text { get; set; }
            public string Script { get; set; }
        }

        public class Translation
        {
            public string Text { get; set; }
            public TextResult Transliteration { get; set; }
            public string To { get; set; }
            public Alignment Alignment { get; set; }
            public SentenceLength SentLen { get; set; }
        }

        public class Alignment
        {
            public string Proj { get; set; }
        }

        public class SentenceLength
        {
            public int[] SrcSentLen { get; set; }
            public int[] TransSentLen { get; set; }
        }

        public async Task<string> DoTranslation(TranslatorInput input)
        {
            logger.LogInformation($"Starting translation ...");

            ValidateStepInput(input);
            
            var output = await PerformTranslationAsync(input);
            // If input was segmented, return segmented output. Otherwise return single translated text.
            var outputStr = input.TranslatorStepConfiguration.IsInputSegmented ? JsonConvert.SerializeObject(output) : output.First().TranslatedText;

            return outputStr;
        }

        private void ValidateStepInput(TranslatorInput input)
        {
            if (input == null)
            {
                logger.LogError($"Argument {nameof(input)} is null");
                throw new ArgumentNullException(nameof(input));
            }

            if (string.IsNullOrEmpty(input.TranslatorStepConfiguration.ServiceConfiguration.Region))
            {
                logger.LogError($"Argument {nameof(input.TranslatorStepConfiguration.ServiceConfiguration.Region)} is null or Empty");
                throw new ArgumentNullException(nameof(input.TranslatorStepConfiguration.ServiceConfiguration.Region));
            }

            if (string.IsNullOrEmpty(input.TranslatorStepConfiguration.ServiceConfiguration.SubscriptionKey))
            {
                logger.LogError($"Argument {nameof(input.TranslatorStepConfiguration.ServiceConfiguration.SubscriptionKey)} is null or Empty");
                throw new ArgumentNullException(nameof(input.TranslatorStepConfiguration.ServiceConfiguration.SubscriptionKey));
            }

            if (string.IsNullOrEmpty(input.TranslatorStepConfiguration.Endpoint))
            {
                logger.LogError($"Argument {nameof(input.TranslatorStepConfiguration.Endpoint)} is null or Empty");
                throw new ArgumentNullException(nameof(input.TranslatorStepConfiguration.Endpoint));
            }

            if (string.IsNullOrEmpty(input.TranslatorStepConfiguration.Route))
            {
                logger.LogError($"Argument {nameof(input.TranslatorStepConfiguration.Route)} is null or Empty");
                throw new ArgumentNullException(nameof(input.TranslatorStepConfiguration.Route));
            }

            if (input.Input == null)
            {
                logger.LogError($"Argument {nameof(input.Input)} is null or Empty");
                throw new ArgumentNullException(nameof(input.Input));
            }
        }

        // Async call to the Translator Text API
        private async Task<ICollection<TranslatorOutputSegment>> PerformTranslationAsync(TranslatorInput input)
        {
            var translatorConfig = input.TranslatorStepConfiguration;

            using (HttpClient client = httpClientFactory.CreateClient())            
            {
                var segmentedOutput = new List<TranslatorOutputSegment>();

                foreach (var segment in input.Input)
                {
                    using (var request = new HttpRequestMessage())
                    {
                        // Build the request.
                        request.Method = HttpMethod.Post;
                        request.Headers.Add("Ocp-Apim-Subscription-Key", translatorConfig.ServiceConfiguration.SubscriptionKey);
                        request.Headers.Add("Ocp-Apim-Subscription-Region", translatorConfig.ServiceConfiguration.Region);
                        request.RequestUri = BuildRequestUri(segment, translatorConfig);

                        TranslationRequestBody[] body = new TranslationRequestBody[]
                        {
                            new TranslationRequestBody() { Text = segment.SourceText } 
                        };
                        var translationResults = await GetTranslationResults(client, request, body);
                        var translationResult = string.Join(" ", translationResults);

                        // Build output segment object
                        var result = new TranslatorOutputSegment(translationResult, segment.SourceLocale, segment.TargetLocale, segment.SegmentID);
                        segmentedOutput.Add(result);
                    }
                }

                return segmentedOutput;
            }
        }

        private Uri BuildRequestUri(TranslatorInputSegment segment, TranslatorConfiguration translatorConfig)
        {
            var route = HttpUtility.ParseQueryString(translatorConfig.Route);

            if (!string.IsNullOrEmpty(segment.SourceLocale))
            {
                route.Set("from", segment.SourceLocale);
            }

            if (!string.IsNullOrEmpty(segment.TargetLocale))
            {
                route.Set("to", segment.TargetLocale);
            }

            ValidateLocales(segment, route);

            return new Uri(translatorConfig.Endpoint + route);
        }

        private void ValidateLocales(TranslatorInputSegment segment, NameValueCollection route)
        {
            var segmentStr = segment.SegmentID >= 0 ? $" for segment {segment.SegmentID}" : "";
            var targetLocaleStr = route["to"];
            var sourceLocaleStr = route["from"];

            if (string.IsNullOrEmpty(targetLocaleStr))
            {
                logger.LogError($"No target locale specified in neither the config Route or the segment input TargetLocale{segmentStr}.");
                throw new ArgumentNullException($"Target locale must be specified in either the configuration Route or the input TargetLocale{segmentStr}.");
            }

            if (string.IsNullOrEmpty(sourceLocaleStr))
            {
                logger.LogInformation($"No source locale specified in neither the config Route or the input SourceLocale{segmentStr}. Source locale will be automatically detected.");
            }

            var targetLocales = targetLocaleStr.Split(",");

            if (targetLocales.Length > 1)
            {
                logger.LogError($"Multiple target locales specified{segmentStr}.");
                throw new ArgumentNullException($"Only one target locale is allowed per translation.");
            }
        }

        private async Task<List<string>> GetTranslationResults(HttpClient client, HttpRequestMessage request, TranslationRequestBody[] body)
        {
            string requestBody = JsonConvert.SerializeObject(body);
            request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

            // Send the request and get response.
            HttpResponseMessage response = await client.SendAsync(request).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                // Read response as a string.
                string result = await response.Content.ReadAsStringAsync();
                TranslationResult[] deserializedOutput = JsonConvert.DeserializeObject<TranslationResult[]>(result);

                List<string> translationResult = new List<string>();

                // Iterate over the deserialized results.
                foreach (TranslationResult o in deserializedOutput)
                {
                    // Print the detected input languge and confidence score.
                    //Console.WriteLine("Detected input language: {0}\nConfidence score: {1}\n", o.DetectedLanguage.Language, o.DetectedLanguage.Score);
                    // Iterate over the results and print each translation.
                    foreach (Translation t in o.Translations)
                    {
                        translationResult.Add(t.Text);
                    }
                }

                return translationResult;
            }
            else
            {
                logger.LogError($"Error during translation. Http status code: {response.StatusCode}");

                throw new Exception("Error calling translator api.");
            }
        }
    }
}
