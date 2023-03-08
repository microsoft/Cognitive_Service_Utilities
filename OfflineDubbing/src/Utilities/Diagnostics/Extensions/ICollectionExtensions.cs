using System;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions
{
    public static class ICollectionExtensions
    {
        public static string ToJSONArray<T>(this ICollection<T> collection)
        {
            return $"[{string.Join(", ", collection).Replace($"{Environment.NewLine}", $"{Environment.NewLine}\t")}{Environment.NewLine}]";
        }
    }
}
