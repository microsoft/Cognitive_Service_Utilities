//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Utilities.Storage;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.STT
{
    public class SpeechConfiguration
    {
        [JsonProperty("ServiceConfiguration")]
        public CognitiveServiceConfiguration ServiceConfiguration { get; set; }

        [JsonProperty("EndpointId")]
        public string EndpointId { get; set; }

        [JsonProperty("Locale")]
        public string Locale { get; set; }

        [JsonProperty("IsDetailedOutputFormat")]
        public bool IsDetailedOutputFormat { get; set; }

        public ContinuousLIDConfiguration ContinuousLID { get; set; }

        public ConversationTranscriptionConfiguration ConversationTranscription { get; set; }

        [JsonProperty("ServiceProperty")]
        public Dictionary<string, string> ServiceProperty { get; set; }

        [JsonProperty("StorageConfiguration")]
        public BlobStorageConfiguration StorageConfiguration { get; set; }

        public SpeechConfiguration()
        {
            IsDetailedOutputFormat = true;
            EndpointId = string.Empty;
            ServiceProperty = new Dictionary<string, string>();
        }
    }

    public class ContinuousLIDConfiguration
    {
        public bool Enabled { get; set; }
        public List<string> CandidateLocales { get; set; }
    }

    public class ConversationTranscriptionConfiguration
    {
        public bool Enabled { get; set; }

        public List<CTSpeaker> Speakers { get; set; }
    }

    public class CTSpeaker
    {
        public string Name { get; set; }

        public string VoiceSample { get; set; }
    }
}
