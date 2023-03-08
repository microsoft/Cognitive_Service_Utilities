//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Linq;

namespace AIPlatform.TestingFramework.STT.TranscriptionUtils
{
    public static class TranscriptionExtensions
    {
        /// <summary>
        /// Returns the array index at which to split this word array according to the pivot given.
        /// </summary>
        /// <param name="timeStamps">The array of words.</param>
        /// <param name="splitOffset">The pivot value to use to calculate the split index.</param>
        /// <returns>The index at which to split this word array according to the pivot. 
        /// All words to the left of this index will have an offset + duration less than the given pivot timespan.</returns>
        public static int GetWordSplitIndex(this TimeStamp[] timeStamps, TimeSpan splitOffset)
        {
            var index = 0;

            while ((index < timeStamps.Length) && (timeStamps[index].Offset +
                   timeStamps[index].Duration < splitOffset))
            {
                index++;
            }

            return index;
        }

        /// <summary>
        /// Gets and returns the selected transcription candidate from all the generated possible candidates.
        /// </summary>
        /// <param name="details">The detailed transcription onject containing all possible transcription candidates.</param>
        /// <returns>The selected transcription candidate from all the generated possible candidates</returns>
        public static NBest GetSelectedNBestResult(this DetailedTranscriptionOutputResultSegment details)
        {
            return details.NBest.Where(candidate => (candidate.DisplayText == details.DisplayText)).First();
        }
    }
}
