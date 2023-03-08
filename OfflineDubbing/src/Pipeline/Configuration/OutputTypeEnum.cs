using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AIPlatform.TestingFramework.Pipeline.Configuration
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum OutputTypeEnum
    {
        Text,
        Json
    }
}
