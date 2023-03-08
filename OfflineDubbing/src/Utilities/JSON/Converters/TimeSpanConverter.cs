//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
using Newtonsoft.Json;
using System;

namespace AIPlatform.TestingFramework.Utilities.JSON.Converters
{
    /// <summary>
    /// TimeSpans are not serialized and deserialized consistently depending on how they're represented. So this 
    /// serializer will ensure the format is maintained no matter what.
    /// </summary>
    public class TimeSpanConverter : JsonConverter<TimeSpan>
    {
        public const string TimeSpanFormatString = "c";

        public override void WriteJson(JsonWriter writer, TimeSpan value, JsonSerializer serializer)
        {
            var timespanFormatted = $"{value.ToString(TimeSpanFormatString)}";
            writer.WriteValue(timespanFormatted);
        }

        public override TimeSpan ReadJson(JsonReader reader, Type objectType, TimeSpan existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            try
            {
                // Try to deserialize span in expected string format
                TimeSpan.TryParseExact((string)reader.Value, TimeSpanFormatString, null, out TimeSpan parsedTimeSpan);
                return parsedTimeSpan;
            }
            catch (InvalidCastException)
            {
                // If this fails, try to deserialize it as if the TimeSpan is represented as the number of ticks in the TimeSpan
                var ticks = (long)reader.Value;
                return TimeSpan.FromTicks(ticks);
            }
        }
    }
}
