//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AIPlatform.TestingFramework.TTSPreProcessing
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TranslationMappingMethodEnum
    {
        StandardScaleAndFit,
        CompensatePauses_AnchorMiddle,
        CompensatePauses_AnchorStart
    }
}
