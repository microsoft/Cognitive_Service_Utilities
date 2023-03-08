using System;

namespace AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions
{
    public static class StringExtensions
    {
        public static string Indent(this string value)
        {
            return value.Replace($"{Environment.NewLine}", $"{Environment.NewLine}\t");
        }
    }
}
