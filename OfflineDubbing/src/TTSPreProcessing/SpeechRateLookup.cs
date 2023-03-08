//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace AIPlatform.TestingFramework.TTSPreProcessing
{
    internal class SpeechRateLookup
    {
        public static Dictionary<string, (double WordRate, double CharRate)> Rate { get; } = new Dictionary<string, (double WordRate, double CharRate)>()
        {
            {"ar", (138, 612) },
            {"zh", (158, 255) },
            {"nl", (202, 978) },
            {"en", (228, 987) },
            {"fi", (161, 1078) },
            {"fr", (195, 998) },
            {"de", (179, 920) },
            {"he", (187, 833) },
            {"it", (188, 950) },
            {"ja", (193, 357) },
            {"pl", (166, 916) },
            {"pt", (181, 913) },
            {"ru", (184, 986) },
            {"sl", (180, 855) },
            {"es", (218, 1025) },
            {"sv", (199, 917) },
            {"tr", (166, 1054) },
        };
    }
}
