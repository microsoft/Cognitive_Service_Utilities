//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.STT.TranscriptionUtils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIPlatform.TestingFramework.STT
{
    public static class SpeechExtensions
    {
        /// <summary>
        /// Creates and returns deep copy of this candidate.
        /// </summary>
        /// <returns>Deep copy of this candidate.</returns>
        public static SpeechCandidate Copy(this SpeechCandidate candidate)
        {
            return new SpeechCandidate()
            {
                LexicalText = candidate.LexicalText,
                Confidence = candidate.Confidence,
                Words = candidate.Words.Select(word => word.Copy()).ToArray()
            };
        }

        /// <summary>
        /// Creates and returns a sub-section of this candidate based on the word indexes provided.
        /// </summary>
        /// <param name="wordStartIndex">The index to begin the sub-section of the current word array.</param>
        /// <param name="wordEndIndex">The index to end the sub-section of the current word array, not including the element at this index.</param>
        /// <returns>A sub-section of this candidate based on the word indexes provided</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static SpeechCandidate SubCandidate(this SpeechCandidate candidate, int wordStartIndex, int wordEndIndex)
        {
            if (candidate.Words.Length <= 0)
            {
                throw new InvalidOperationException("No words to create subsection of speech candidate");
            }

            if (wordStartIndex < 0 || wordEndIndex > candidate.Words.Length)
            {
                throw new ArgumentException("Word indexes cannot be less than 0 or more than the word array length.");
            }

            var subCandidate = new SpeechCandidate()
            {
                Confidence = candidate.Confidence,
                Words = candidate.Words[wordStartIndex..wordEndIndex]
            };

            List<string> subCandidateWords = new List<string>();
            for (int i = wordStartIndex; i < wordEndIndex; i++)
            {
                subCandidateWords.Add(candidate.Words[i].Word);
            }

            subCandidate.LexicalText = string.Join(" ", subCandidateWords);

            return subCandidate;
        }

        /// <summary>
        /// Splits this candidate into two subcandidates based on the provided pivot offset and returns the result.
        /// </summary>
        /// <param name="splitOffset"></param>
        /// <returns>A tuple consisting of the two candidates split from this candidate.</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static (SpeechCandidate firstCandidate, SpeechCandidate secondCandidate) Split(this SpeechCandidate candidate, TimeSpan splitOffset)
        {
            if (candidate.Words.Length <= 0)
            {
                throw new InvalidOperationException("No words to split in speech candidate");
            }

            if (splitOffset.Ticks < 0 || splitOffset < candidate.Words.First().Offset || splitOffset > candidate.Words.Last().Offset + candidate.Words.Last().Duration)
            {
                throw new ArgumentException("splitOffset must be within start and end of the candidate.");
            }

            int splitWordPointer = candidate.Words.GetWordSplitIndex(splitOffset);

            // create first candidate
            var firstCandidate = candidate.SubCandidate(0, splitWordPointer);

            // create second candidate
            if (splitWordPointer >= candidate.Words.Length)
            {
                return (firstCandidate, null);
            }

            var secondCandidate = candidate.SubCandidate(splitWordPointer, candidate.Words.Length);

            return (firstCandidate, secondCandidate);
        }

        /// <summary>
        /// Appends the data from the given segment to this segment.
        /// The resulting NBest is undefined.
        /// </summary>
        /// <param name="segment">The segment to append to this segment.</param>
        public static void Append(this SpeechOutputSegment currentSegment, SpeechOutputSegment additionSegment)
        {
            if (additionSegment.IdentifiedSpeaker != currentSegment.IdentifiedSpeaker ||
                additionSegment.IdentifiedLocale != currentSegment.IdentifiedLocale ||
                additionSegment.IdentifiedEmotion != currentSegment.IdentifiedEmotion)
            {
                throw new ArgumentException($"Can't append a segment with unequal speaker, locale or emotion. This segment: " +
                    $"{nameof(currentSegment.IdentifiedSpeaker)}: {currentSegment.IdentifiedSpeaker}, {nameof(currentSegment.IdentifiedLocale)}: {currentSegment.IdentifiedLocale}, {nameof(currentSegment.IdentifiedEmotion)}: {currentSegment.IdentifiedEmotion} " +
                    $"Segment to append: {nameof(additionSegment.IdentifiedSpeaker)}: {additionSegment.IdentifiedSpeaker}, {nameof(additionSegment.IdentifiedLocale)}: {additionSegment.IdentifiedLocale}, " +
                    $"{nameof(additionSegment.IdentifiedEmotion)}: {additionSegment.IdentifiedEmotion}");
            }

            currentSegment.LexicalText += $" {additionSegment.LexicalText}";
            currentSegment.DisplayText += $" {additionSegment.DisplayText}";
            currentSegment.Duration = (additionSegment.Offset - currentSegment.Offset) + additionSegment.Duration;
            currentSegment.TimeStamps = currentSegment.TimeStamps.Concat(additionSegment.TimeStamps).ToList();
            currentSegment.DisplayWordTimeStamps = currentSegment.DisplayWordTimeStamps.Concat(additionSegment.DisplayWordTimeStamps).ToList();
            currentSegment.NBest = null;
        }

        /// <summary>
        /// Creates and returns a sub-section of this segment based on the word and display word indexes provided.
        /// </summary>
        /// <param name="wordStartIndex">The index to begin the subsegment in the current word array.</param>
        /// <param name="wordEndIndex">The index to end the subsegment in the current word array, not including the element at this index.</param>
        /// <param name="displayWordStartIndex">The index to begin the subsegment in the current display word array.</param>
        /// <param name="displayWordEndIndex">The index to end the subsegment in the current display word array, not icluding the element at this index.</param>
        /// <param name="nBestWordIndexes">Collection of tuples containing the start and end indexes for the subsegments to be extracted from the current collection of lexical speech candidates. Ending indexes are not-inclusive.</param>
        /// <returns>A sub-section of this segment based on the word and display word indexes provided.</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static SpeechOutputSegment SubSegment(this SpeechOutputSegment segment, int wordStartIndex, int wordEndIndex, int displayWordStartIndex, int displayWordEndIndex, ICollection<(int, int)> nBestWordIndexes)
        {
            if (segment.TimeStamps.Count <= 0 || segment.DisplayWordTimeStamps.Count <= 0)
            {
                throw new InvalidOperationException("No words or display words to create subsection of speech segment.");
            }

            if (wordStartIndex < 0 || wordEndIndex > segment.TimeStamps.Count)
            {
                throw new ArgumentException("Word indexes cannot be less than 0 or more than timestamps collection count.");
            }

            if (displayWordStartIndex < 0 || displayWordEndIndex > segment.DisplayWordTimeStamps.Count)
            {
                throw new ArgumentException("Display word indexes cannot be less than 0 or more than display word timestamps collection count.");
            }

            TimeStamp[] lexicalTimeStamps = segment.TimeStamps.ToArray();
            TimeStamp[] displayTimeStamps = segment.DisplayWordTimeStamps.ToArray();

            var subSegment = new SpeechOutputSegment()
            {
                Offset = lexicalTimeStamps[wordStartIndex].Offset,
                Duration = lexicalTimeStamps[wordEndIndex - 1].Duration + lexicalTimeStamps[wordEndIndex - 1].Offset - lexicalTimeStamps[wordStartIndex].Offset,
                IdentifiedLocale = segment.IdentifiedLocale,
                IdentifiedEmotion = segment.IdentifiedEmotion,
                IdentifiedSpeaker = segment.IdentifiedSpeaker,
                TimeStamps = new List<TimeStamp>(lexicalTimeStamps[wordStartIndex..wordEndIndex]),
                DisplayWordTimeStamps = new List<TimeStamp>(displayTimeStamps[displayWordStartIndex..displayWordEndIndex])
            };

            List<string> subSegmentLexicalString = new List<string>();
            for (int i = wordStartIndex; i < wordEndIndex; i++)
            {
                subSegmentLexicalString.Add(lexicalTimeStamps[i].Word);
            }

            List<string> subSegmentDisplayString = new List<string>();
            for (int i = displayWordStartIndex; i < displayWordEndIndex; i++)
            {
                subSegmentDisplayString.Add(displayTimeStamps[i].Word);
            }

            subSegment.LexicalText = string.Join(" ", subSegmentLexicalString);
            subSegment.DisplayText = string.Join(" ", subSegmentDisplayString);

            List<SpeechCandidate> subSegmentNBest = new List<SpeechCandidate>();
            for (int i = 0; i < segment.NBest.Count; i++)
            {
                var candidate = segment.NBest.ElementAt(i);
                (int nBestWordStartIndex, int nBestWordEndIndex) = nBestWordIndexes.ElementAt(i);
                subSegmentNBest.Add(candidate.SubCandidate(nBestWordStartIndex, nBestWordEndIndex));
            }
            subSegment.NBest = subSegmentNBest;

            return subSegment;
        }

        /// <summary>
        /// Splits this segment into two subsegments based on the provided pivot offset and returns the result.
        /// </summary>
        /// <param name="splitOffset">The pivot to use to make the partition.</param>
        /// <returns>A tuple consisting of the two segments split from this segment.</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static (SpeechOutputSegment firstSegment, SpeechOutputSegment secondSegment) SplitSegment(this SpeechOutputSegment segment, TimeSpan splitOffset)
        {
            if (segment.TimeStamps.Count <= 0 || segment.DisplayWordTimeStamps.Count <= 0)
            {
                throw new InvalidOperationException("No words or display words to split speech segment.");
            }

            if (splitOffset.Ticks < 0 || splitOffset < segment.Offset || splitOffset > segment.Offset + segment.Duration)
            {
                throw new ArgumentException("splitOffset must be within start and end of current segment");
            }

            TimeStamp[] lexicalTimeStamps = segment.TimeStamps.ToArray();
            TimeStamp[] displayTimeStamps = segment.DisplayWordTimeStamps.ToArray();

            var currLexicalTimeStampPointer = lexicalTimeStamps.GetWordSplitIndex(splitOffset);
            var currDisplayTimeStampPointer = displayTimeStamps.GetWordSplitIndex(splitOffset);
            var nBestWordTimeStampPointers = new List<int>();
            foreach (SpeechCandidate speechCandidate in segment.NBest)
            {
                var nBestWordTimeStampPointer = speechCandidate.Words.GetWordSplitIndex(splitOffset);
                nBestWordTimeStampPointers.Add(nBestWordTimeStampPointer);
            }

            // create first segment

            var firstNBestWordPointers = new List<(int, int)>();

            for (int i = 0; i < segment.NBest.Count; i++)
            {
                firstNBestWordPointers.Add((0, nBestWordTimeStampPointers[i]));
            }

            var firstSegment = segment.SubSegment(0, currLexicalTimeStampPointer, 0, currDisplayTimeStampPointer, firstNBestWordPointers);

            // create second segment

            var secondNBestWordPointers = new List<(int, int)>();

            for (int i = 0; i < segment.NBest.Count; i++)
            {
                var candidateWordsLength = segment.NBest.ElementAt(i).Words.Length;
                secondNBestWordPointers.Add((nBestWordTimeStampPointers[i], candidateWordsLength));
            }

            var secondSegment = segment.SubSegment(currLexicalTimeStampPointer, lexicalTimeStamps.Length, currDisplayTimeStampPointer, displayTimeStamps.Length, secondNBestWordPointers);

            return (firstSegment, secondSegment);
        }

        /// <summary>
        /// Creates and returns deep copy of this segment with the specified segment ID.
        /// </summary>
        /// <returns>Deep copy of this segment with the specified segment ID</returns>
        public static SpeechOutputSegment Copy(this SpeechOutputSegment segment, int segmentId)
        {
            ICollection<TimeStamp> displayWordTimeStamps = segment.DisplayWordTimeStamps == null ? new List<TimeStamp>() : segment.DisplayWordTimeStamps;

            return new SpeechOutputSegment()
            {
                SegmentID = segmentId,
                LexicalText = segment.LexicalText,
                DisplayText = segment.DisplayText,
                IdentifiedSpeaker = segment.IdentifiedSpeaker,
                IdentifiedLocale = segment.IdentifiedLocale,
                IdentifiedEmotion = segment.IdentifiedEmotion,
                Duration = segment.Duration,
                Offset = segment.Offset,
                TimeStamps = new List<TimeStamp>(segment.TimeStamps.ToList()),
                DisplayWordTimeStamps = displayWordTimeStamps,
                NBest = segment.NBest.Select(candidate => candidate.Copy()).ToList()
            };
        }

        /// <summary>
        /// Gets and returns the selected transcription candidate from all the generated possible candidates for this segment.
        /// </summary>
        /// <param name="segment">The speech-to-text output segment.</param>
        /// <returns>The selected transcription candidate from all the generated possible candidates for this segment.</returns>
        public static SpeechCandidate GetSelectedCandidate(this SpeechOutputSegment segment)
        {
            return segment.NBest.Where(candidate => (candidate.LexicalText == segment.LexicalText)).FirstOrDefault();
        }
    }
}
