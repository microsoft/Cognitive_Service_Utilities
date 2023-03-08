using System.Collections.Generic;
using System.Linq;

namespace AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions
{
    public static class IDictionaryExtensions
    {
        public static IDictionary<TKey, TValue> AddIfNotNull<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
            where TValue : class
        {
            if (value != null) 
            {
                dictionary.Add(key, value);
            }

            return dictionary;
        }

        public static IDictionary<TKey0, object> AddDictionaryStringIfNotNullOrEmpty<TKey0, TKey1, TValue1>(this IDictionary<TKey0, object> dictionary, TKey0 key, IDictionary<TKey1, TValue1> dictionaryVal)
        {
            if (dictionaryVal != default && dictionaryVal.Count != 0)
            {
                dictionary.Add(key, dictionaryVal.ToConsoleString());
            }

            return dictionary;
        }

        public static string ToConsoleString<TKey, TValue>(this IDictionary<TKey, TValue> dictionary)
        {
            return $"{{{string.Join(", ", dictionary.Select(kv => kv.Key + ": " + kv.Value).ToArray())}}}";
        }

        public static bool IsEqualToDictionary<TKey, TValue>(this IDictionary<TKey, TValue> x, IDictionary<TKey, TValue> y, IEqualityComparer<TValue> valueComparer = null)
        {
            valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
            
            if (x.Count != y.Count)
            {
                return false;
            }
            if (x.Keys.Except(y.Keys).Any())
            {
                return false;
            }
            if (y.Keys.Except(x.Keys).Any())
            {
                return false;
            }
            foreach (var pair in x)
            {
                if (!valueComparer.Equals(pair.Value, y[pair.Key]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
