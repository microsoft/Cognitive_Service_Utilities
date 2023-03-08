using Newtonsoft.Json;
using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Utilities.Storage;

// all the data needed to connect to the Azure Text to Speech
namespace AIPlatform.TestingFramework.TTS
{
    public class TextToSpeechConfiguration
    {
        [JsonProperty("ServiceConfiguration")]
        public CognitiveServiceConfiguration ServiceConfiguration { get; set; }

        [JsonProperty("StorageConfiguration")]
        public BlobStorageConfiguration StorageConfiguration { get; set; }
    }
}
