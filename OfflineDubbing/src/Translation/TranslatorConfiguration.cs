using AIPlatform.TestingFramework.Common;
using Newtonsoft.Json;
using System;

namespace AIPlatform.TestingFramework.Translation
{
    public class TranslatorConfiguration 
    {
        [JsonProperty("ServiceConfiguration")]
        public CognitiveServiceConfiguration ServiceConfiguration { get; set; }

        [JsonProperty("Route")]
        public string Route { get; set; }

        [JsonProperty("Endpoint")]
        public string Endpoint { get; set; }

        [JsonProperty("IsInputSegmented")]
        public bool IsInputSegmented { get; set; } = false;
                
        public TranslatorConfiguration()
        {
            Endpoint = "https://api.cognitive.microsofttranslator.com";
            Route = "/translate?api-version=3.0";
        }

        override
        public string ToString()
        {
            return $"{{ {Environment.NewLine}\tIsInputSegmented: {IsInputSegmented}, {Environment.NewLine}\tEndPoint: {Endpoint}, {Environment.NewLine}\tRoute: {Route}, {Environment.NewLine}\tRegion: {ServiceConfiguration.Region}{Environment.NewLine}}}";
        }
    }
}
