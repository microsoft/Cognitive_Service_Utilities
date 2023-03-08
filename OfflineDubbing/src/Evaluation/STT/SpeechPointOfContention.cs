//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Diagnostics.CodeAnalysis;

namespace AIPlatform.TestingFramework.Evaluation.STT
{
    public enum ContentionType
    {
        WrongWord, MissingWord, InsertedWord
    }

    public class SpeechPointOfContention : IEquatable<SpeechPointOfContention>
    {
        [JsonProperty("Word", Required = Required.Always)]
        public readonly string Word;

        [JsonProperty("WordIndex", Required = Required.Always)]
        public readonly int WordIndex;

        [JsonProperty("ContentionType", Required = Required.Always)]
        [JsonConverter(typeof(StringEnumConverter))]
        public readonly ContentionType ContentionType;

        public SpeechPointOfContention(
            string word,
            int wordIndex,
            ContentionType operation)
        {
            Word = word;
            WordIndex = wordIndex;
            ContentionType = operation;
        }

        public static bool operator ==(SpeechPointOfContention poc1, SpeechPointOfContention poc2)
        {
            if (ReferenceEquals(poc1, poc2)) return true;
            if (ReferenceEquals(poc1, null)) return false;
            if (ReferenceEquals(poc2, null)) return false;

            return poc1.Equals(poc2);
        }

        public static bool operator !=(SpeechPointOfContention poc1, SpeechPointOfContention poc2)
        {
            if (ReferenceEquals(poc1, poc2)) return false;
            if (ReferenceEquals(poc1, null)) return true;
            if (ReferenceEquals(poc2, null)) return true;

            return !poc1.Equals(poc2);
        }

        public bool Equals([AllowNull] SpeechPointOfContention other)
        {
            return Word == other.Word && WordIndex == other.WordIndex && ContentionType == other.ContentionType;
        }

        public override int GetHashCode() => unchecked(17 * Word.GetHashCode() + WordIndex.GetHashCode() + ContentionType.GetHashCode());

        public override bool Equals(object obj)
        {
            var poc = obj as SpeechPointOfContention;
            if (poc != null)
            {
                return Equals(poc);
            }
            else
            {
                return false;
            }
        }
    }
}
