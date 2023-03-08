//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.Evaluation.Interfaces;
using AIPlatform.TestingFramework.ExecutionPipeline.Execution;
using AIPlatform.TestingFramework.STT;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace AIPlatform.TestingFramework.Evaluation.STT
{
    public class SpeechCorrectnessEvaluator : ExecutePipelineStep, ISpeechCorrectnessEvaluator
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;

        public SpeechCorrectnessEvaluator(IStorageManager storageManager, IOrchestratorLogger<TestingFrameworkOrchestrator> logger) : base(logger, storageManager)
        {
            this.logger = logger;
        }

        public string EvaluateCorrectness(SpeechCorrectnessInput input)
        {
            ValidateInput(input);

            logger.LogInformation("Starting speech-to-text correctness evaluation...");

            var speechOutputSegments = input.Input;
            var lowerConfidenceCandidateThreshold = input.Configuration.ConfidenceThreshold;
            var errorOccurrenceThreshold = input.Configuration.OccurrenceThreshold;
            var speechCorrectnessOutputSegments = new List<SpeechCorrectnessOutputSegment>();            

            foreach (var speechOutputSegment in speechOutputSegments)
            {
                // Filter the candidates within the lower confidence candidate threshold
                var filteredCandidates = GetFilteredCandidates(speechOutputSegment, lowerConfidenceCandidateThreshold);

                ICollection<SpeechPointOfContention> interventions = null;
                if (filteredCandidates.Count > 0)
                {
                    // Calculate the possible reasons for needed intervention based on the selected text, the filtered candidates, and the error threshold
                    interventions = CalculateInterventionReasons(speechOutputSegment.LexicalText, filteredCandidates, errorOccurrenceThreshold);
                }

                // Build speech correctness segment
                SpeechCorrectnessOutputSegment correctionSegment = new SpeechCorrectnessOutputSegment()
                {
                    SegmentID = speechOutputSegment.SegmentID
                };
                if (interventions != null && interventions.Count > 0)
                {
                    correctionSegment.InterventionNeeded = true;
                    correctionSegment.InterventionReasons = interventions;
                }
                else
                {
                    correctionSegment.InterventionNeeded = false;
                    correctionSegment.InterventionReasons = null;
                }

                speechCorrectnessOutputSegments.Add(correctionSegment);
            }

            return JsonConvert.SerializeObject(speechCorrectnessOutputSegments);
        }

        private void ValidateInput(SpeechCorrectnessInput input)
        {
            if (input == null)
            {
                logger.LogError($"Argument {nameof(input)} is null");
                throw new ArgumentNullException(nameof(input));
            }

            if (input.Input == null)
            {
                logger.LogError($"Argument {nameof(input.Input)} is null");
                throw new ArgumentNullException(nameof(input.Input));
            }

            if (input.Configuration == null)
            {
                logger.LogError($"Argument {nameof(input.Configuration)} is null");
                throw new ArgumentNullException(nameof(input.Configuration));
            }

            if (input.Configuration.ConfidenceThreshold > 1.0 || input.Configuration.ConfidenceThreshold < 0.0)
            {
                logger.LogError($"Argument {nameof(input.Configuration.ConfidenceThreshold)} must be a value between 0.0 and 1.0.");
                throw new ArgumentException($"{nameof(input.Configuration.ConfidenceThreshold)} must be a value between 0.0 and 1.0.");
            }

            if (input.Configuration.OccurrenceThreshold > 1.0 || input.Configuration.OccurrenceThreshold < 0.0)
            {
                logger.LogError($"Argument {nameof(input.Configuration.OccurrenceThreshold)} must be a value between 0.0 and 1.0.");
                throw new ArgumentException($"{nameof(input.Configuration.OccurrenceThreshold)} must be a value between 0.0 and 1.0.");
            }
        }

        private ICollection<SpeechCandidate> GetFilteredCandidates(SpeechOutputSegment segment, double lowerConfidenceCandidateThreshold)
        {
            var selectedCandidate = segment.GetSelectedCandidate();
            if (selectedCandidate == default)
            {
                throw new ArgumentException($"Selected transcription is not included in the NBest candidates for segment {segment.SegmentID}.");
            }

            var sortedLowerConfidenceCandidates = new Dictionary<SpeechCandidate, float>();
            var filteredCandidates = new List<SpeechCandidate>();

            foreach (var candidate in segment.NBest)
            {
                // Add all candidates with a confidence score higher than the selected candidate's confidence score
                if (candidate.Confidence >= selectedCandidate.Confidence && !ReferenceEquals(candidate, selectedCandidate))
                {
                    filteredCandidates.Add(candidate);
                }

                // Sort candidates with a confidence score lower than the selected candidate's confidence score by their confidence scores
                if (candidate.Confidence < selectedCandidate.Confidence)
                {
                    sortedLowerConfidenceCandidates.Add(candidate, candidate.Confidence);
                }
            }

            // Take the top-confidence candidates of lower confidence determined by the threshold %
            var numCandidatesWithinThreshold = (int)(sortedLowerConfidenceCandidates.Count * lowerConfidenceCandidateThreshold);
            var candidatesWithinThreshold = sortedLowerConfidenceCandidates.OrderByDescending((lowerConfPair) => lowerConfPair.Value).Take(numCandidatesWithinThreshold).Select((lowerConfPair) => lowerConfPair.Key);
            filteredCandidates.AddRange(candidatesWithinThreshold);

            return filteredCandidates;
        }

        private ICollection<SpeechPointOfContention> CalculateInterventionReasons(string selectedText, ICollection<SpeechCandidate> candidates, double errorOccurrenceThreshold)
        {
            ICollection<ICollection<ICollection<SpeechPointOfContention>>> allPossiblePointsOfContention = new List<ICollection<ICollection<SpeechPointOfContention>>>();

            foreach (var candidate in candidates)
            {
                // Get points of contention for all speech candidates in this segment compared to the text selected
                var candidateSentence = candidate.LexicalText.Split(' ');
                var segmentSentence = selectedText.Split(' ');
                var editDistanceResults = CalculateEditDistance(candidateSentence, segmentSentence);
                var allPossibleCandidatePointsOfContention = GetAllPossiblePointsOfContention(
                    candidateSentence,
                    segmentSentence,
                    new Stack<SpeechPointOfContention>(), 
                    editDistanceResults);
                allPossiblePointsOfContention.Add(allPossibleCandidatePointsOfContention);
            }

            // Calculate which combination of all possible points of contention will lead to the highest count for the selected points of contention across all candidates
            var bestCandidatePointsOfContention = GetBestContentionCombination(allPossiblePointsOfContention);

            // Keep count of occurrences of all points of contention across the candidates
            var candidatePOCCounts = new Dictionary<SpeechPointOfContention, int>();
            foreach (var pointsOfContention in bestCandidatePointsOfContention)
            {
                CountPointsOfContention(pointsOfContention, candidatePOCCounts);
            }

            var filteredPointsOfContention = new List<SpeechPointOfContention>();
            foreach (var pocToCount in candidatePOCCounts)
            {
                // For each point of contention, calculate its rate of occurrence relative to the number of candidates
                var occurrenceRate = (double)pocToCount.Value / candidates.Count;
                // If the rate of occurrence is within the threshold, we'll count the point of contention as an error needing intervention
                if (occurrenceRate >= errorOccurrenceThreshold)
                {
                    filteredPointsOfContention.Add(pocToCount.Key);
                }
            }

            return filteredPointsOfContention;
        }

        /// <summary>
        /// Calculates the edit or Levenshtein distance in terms of the words between the source sentence and the target sentence using a dynamic programming approach and returns the resulting array.
        /// </summary>
        /// <param name="source">List of words in source sentence.</param>
        /// <param name="target">List of words in target sentence.</param>
        /// <returns>The resulting array from solving the problem using dynamic programming.</returns>
        private int[,] CalculateEditDistance(string[] source, string[] target)
        {
            var srcLen = source.Length;
            var tgtLen = target.Length;
            var results = new int[tgtLen + 1, srcLen + 1]; // Array to memoize result of previous computations

            // Base case: target text is empty, we insert all words
            for (var i = 0; i <= srcLen; i++)
            {
                results[0, i] = i;
            }

            // Base case: source text is empty, we remove all words
            for (var i = 0; i <= tgtLen; i++)
            {
                results[i, 0] = i;
            }

            for (int tgtIndex = 1; tgtIndex <= tgtLen; tgtIndex++)
            {
                for (int srcIndex = 1; srcIndex <= srcLen; srcIndex++)
                {
                    if (source[srcIndex - 1] == target[tgtIndex - 1])
                    {
                        // If word from both texts is same then we do not perform any operation
                        results[tgtIndex, srcIndex] = results[tgtIndex - 1, srcIndex - 1];
                    }
                    else
                    {
                        // If word from both texts is not same then we take the minimum from three possible operations (insert, edit, replace) + 1
                        var inserted = results[tgtIndex - 1, srcIndex];
                        var missing = results[tgtIndex, srcIndex - 1];                                             
                        var different = results[tgtIndex - 1, srcIndex - 1];
                        results[tgtIndex, srcIndex] = 1 + Math.Min(Math.Min(missing, inserted), different);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Generates and returns all possible combinations of the points of contention between the source sentence and the target sentence.
        /// The results are generated recursively from the dynamic programming array generated by CalculateEditDistance.
        /// </summary>
        /// <param name="source">The source sentence expressed as an array of words.</param>
        /// <param name="target">The target sentence expressed as an array of words.</param>
        /// <param name="pointsOfContention">A Stack containing all the points of contention for the current path being explored.</param>
        /// <param name="editDistanceResults">The dynamic programming array generated by calling CalculateEditDistance with the specified source and target.</param>
        /// <returns>All possible combinations of the points of contention between the source sentence and the target sentence.</returns>
        private ICollection<ICollection<SpeechPointOfContention>> GetAllPossiblePointsOfContention(
            string[] source,
            string[] target,
            Stack<SpeechPointOfContention> pointsOfContention,
            int[,] editDistanceResults)
        {
            var srcIndex = source.Length;
            var tgtIndex = target.Length;
            var allPossiblePointsOfContention  = new List<ICollection<SpeechPointOfContention>>();

            while (true)
            {
                if (srcIndex == 0 || tgtIndex == 0)
                {
                    while (srcIndex > 0)
                    {
                        // Only words left are in the source, mark all words left as deletions
                        pointsOfContention.Push(new SpeechPointOfContention(source[srcIndex - 1], srcIndex - 1, ContentionType.MissingWord));
                        srcIndex--;
                    }

                    while (tgtIndex > 0)
                    {
                        // Only words left are in the target, mark all words left as insertions
                        pointsOfContention.Push(new SpeechPointOfContention(target[tgtIndex - 1], tgtIndex - 1, ContentionType.InsertedWord));
                        tgtIndex--;
                    }

                    // Add current points of contention to collection of all points of contention for this candidate
                    allPossiblePointsOfContention.Add(pointsOfContention.ToList());
                    break;
                }

                // If the word in source and target is the same, proceed diagonally through result array
                if (source[srcIndex - 1] == target[tgtIndex - 1])
                {
                    srcIndex--;
                    tgtIndex--;
                }
                else
                {
                    bool editedWord = false, deletedWord = false;

                    // Check if word in source is different from target
                    if (editDistanceResults[tgtIndex, srcIndex] == editDistanceResults[tgtIndex - 1, srcIndex - 1] + 1)
                    {
                        // If so, mark the word as an edit
                        pointsOfContention.Push(new SpeechPointOfContention(source[srcIndex - 1], tgtIndex - 1, ContentionType.WrongWord));
                        srcIndex--;
                        tgtIndex--;
                        editedWord = true;
                    }

                    // Check if there's an extra word in source that's missing in target
                    if (srcIndex > 0 && editDistanceResults[tgtIndex, srcIndex] == editDistanceResults[tgtIndex, srcIndex - 1] + 1)
                    {
                        if (!editedWord)
                        {
                            pointsOfContention.Push(new SpeechPointOfContention(source[srcIndex - 1], srcIndex - 1, ContentionType.MissingWord));
                            srcIndex--;
                        }
                        else
                        {
                            // Two points of contention are true: a word is missing in target compared to the source, but you could also see it
                            // as the word in source being an edit from the word in target. We have to explore the missing word as a different path.
                            // Note that the double instantiation of the stack will ensure we keep the original stack order.
                            var newPointsOfContention = new Stack<SpeechPointOfContention>(new Stack<SpeechPointOfContention>(pointsOfContention));
                            // Replace the "wrong word" contention in the new path with "missing word"
                            newPointsOfContention.Pop();
                            newPointsOfContention.Push(new SpeechPointOfContention(source[srcIndex], srcIndex, ContentionType.MissingWord));
                            // Initiate recursive instance of the "missing word" path with remaining words
                            allPossiblePointsOfContention.AddRange(GetAllPossiblePointsOfContention(source[0..srcIndex], target[0..(tgtIndex + 1)], newPointsOfContention, editDistanceResults));
                        }

                        deletedWord = true;
                    }

                    // Check if there's a word not in source that's inserted in target
                    if (tgtIndex > 0 && editDistanceResults[tgtIndex, srcIndex] == editDistanceResults[tgtIndex - 1, srcIndex] + 1)
                    {
                        if (!editedWord && !deletedWord)
                        {
                            pointsOfContention.Push(new SpeechPointOfContention(target[tgtIndex - 1], tgtIndex - 1, ContentionType.InsertedWord));
                            tgtIndex--;
                        }
                        else
                        {
                            // We have to explore the inserted word as a different path
                            var newPointsOfContention = new Stack<SpeechPointOfContention>(new Stack<SpeechPointOfContention>(pointsOfContention));
                            // Replace the last contention in the new path with "inserted word"
                            newPointsOfContention.Pop();
                            newPointsOfContention.Push(new SpeechPointOfContention(target[tgtIndex], tgtIndex, ContentionType.InsertedWord));
                            // Initiate recursive instance of the "inserted word" path with remaining words
                            allPossiblePointsOfContention.AddRange(GetAllPossiblePointsOfContention(source[0..(srcIndex + 1)], target[0..tgtIndex], newPointsOfContention, editDistanceResults));
                        }
                    }
                }
            }

            return allPossiblePointsOfContention;
        }

        /// <summary>
        /// Takes all possible paths for points of contention for all candidates and chooses one for each candidate that maximizes the count of these points of contention
        /// across all the candidates in the segment.
        /// </summary>
        /// <param name="pointsOfContention">All possible paths for points of contention for all candidates.</param>
        /// <returns>Collection of chosen points of contention for each candidate that maximizes the counts of each point of contention across the candidates.</returns>
        private ICollection<ICollection<SpeechPointOfContention>> GetBestContentionCombination(ICollection<ICollection<ICollection<SpeechPointOfContention>>> pointsOfContention)
        {
            var linkedPointsOfContention = BuildSortedLinkedPointsOfContention(pointsOfContention);
            var bestContentionCombination = new HashSet<ICollection<SpeechPointOfContention>>();

            // Go through each of the combination of possible points of contention for each candidate
            foreach (var possibleCandidatePOCs in linkedPointsOfContention)
            {
                // Skip these candidate combinations if one has already been chosen for this candidate
                if (!possibleCandidatePOCs.Any(candidatesPOC => bestContentionCombination.Contains(candidatesPOC.pointsOfContention)))
                {
                    // Choose the one with the highest count of matching points of contention
                    var highestPossibleCandidatesPOC = possibleCandidatePOCs.First();
                    bestContentionCombination.Add(highestPossibleCandidatesPOC.pointsOfContention);

                    // Include all linked points of contention
                    foreach (var linkedCandidatePOC in highestPossibleCandidatesPOC.linkedCandidatePOCs)
                    {
                    bestContentionCombination.Add(linkedCandidatePOC.pointsOfContention);
                    }
                }                
            }

            return bestContentionCombination;
        }

        private ICollection<ICollection<PossibleCandidatePointsOfContention>> BuildSortedLinkedPointsOfContention(ICollection<ICollection<ICollection<SpeechPointOfContention>>> pointsOfContention)
        {
            var linkedPointsOfContention = new List<ICollection<PossibleCandidatePointsOfContention>>();

            // For each possible collection of points of contention for each candidate, link each possible collection with other candidate's collections
            // if the other candidate's collection contains a point of contention matching one in the current candidate's collection of point of contentions.
            // For each candidate that is linked, sort all possibilities by the number of possible contentions matching the contentions in the candidate.
            for (var i = 0; i < pointsOfContention.Count; i++)
            {
                var allPossibleLinkedCandidatePOC = new Dictionary<ICollection<SpeechPointOfContention>, PossibleCandidatePointsOfContention>();
                for (var j = 0; j < pointsOfContention.Count; j++)
                {
                    if (i != j)
                    {
                        var allPossibleCandidatePOC1 = pointsOfContention.ElementAt(i);
                        var allPossibleCandidatePOC2 = pointsOfContention.ElementAt(j);                        

                        for (var k = 0; k < allPossibleCandidatePOC1.Count; k++)
                        {
                            if (!allPossibleLinkedCandidatePOC.TryGetValue(allPossibleCandidatePOC1.ElementAt(k), out var candidatePossiblePOC1))
                            {
                                candidatePossiblePOC1 = new PossibleCandidatePointsOfContention(allPossibleCandidatePOC1.ElementAt(k));
                                allPossibleLinkedCandidatePOC.Add(allPossibleCandidatePOC1.ElementAt(k), candidatePossiblePOC1);
                            }

                            // Compare all possibilities for other candidates and link them to this candidate if
                            // there's any matches in the points of contention. If there's multiple matches, choose
                            // the grouping with the highest matching points of contention
                            var maxMatchingPOCCount = 0;
                            PossibleCandidatePointsOfContention pointsOfContentionToLink = null;

                            for (var l = 0; l < allPossibleCandidatePOC2.Count; l++)
                            {
                                var candidatePossiblePOC2 = new PossibleCandidatePointsOfContention(allPossibleCandidatePOC2.ElementAt(l));
                                var matchingPOCCount = candidatePossiblePOC1.GetMatchingPointsOfContentionCount(candidatePossiblePOC2);
                                if (matchingPOCCount > maxMatchingPOCCount)
                                {
                                    maxMatchingPOCCount = matchingPOCCount;
                                    pointsOfContentionToLink = candidatePossiblePOC2;
                                }
                            }

                            if (pointsOfContentionToLink != null)
                            {
                                candidatePossiblePOC1.LinkCandidate(pointsOfContentionToLink);
                            }
                        }
                    }

                    if (allPossibleLinkedCandidatePOC.Count == 0)
                    {
                        // If there were no other candidates to compare to, choose arbitrarily
                        var onlyPossibleCandidatePOC = pointsOfContention.ElementAt(i);
                        var candidatePOC = new PossibleCandidatePointsOfContention(onlyPossibleCandidatePOC.First());
                        allPossibleLinkedCandidatePOC.Add(onlyPossibleCandidatePOC.First(), candidatePOC);
                }
                }

                // Sort list of possible points of contention for this candidate by the number of possible contentions matching the contentions in the candidate
                // and add it to the list for all candidates
                var allPossibleLinkedCandidatePOCList = allPossibleLinkedCandidatePOC.Values.ToList();
                allPossibleLinkedCandidatePOCList.Sort((a, b) => b.GetMatchingPointsOfContentionInLinkedCandidates().CompareTo(a.GetMatchingPointsOfContentionInLinkedCandidates()));
                linkedPointsOfContention.Add(allPossibleLinkedCandidatePOCList);
            }

            // Sort the list of all linked possible points of contention for all candidates by the max number of possible contentions matching the contentions in the candidate
            linkedPointsOfContention.Sort((a, b) => b.First().GetMatchingPointsOfContentionInLinkedCandidates().CompareTo(a.First().GetMatchingPointsOfContentionInLinkedCandidates()));

            return linkedPointsOfContention;
        }

        private void CountPointsOfContention(ICollection<SpeechPointOfContention> pointsOfContention, Dictionary<SpeechPointOfContention, int> allPointsOfContention)
        {
            foreach (SpeechPointOfContention pointOfContention in pointsOfContention)
            {
                if (allPointsOfContention.TryGetValue(pointOfContention, out var count))
                {
                    allPointsOfContention[pointOfContention] = ++count;
                }
                else
                {
                    allPointsOfContention.Add(pointOfContention, 1);
                }
            }
        }

        private class PossibleCandidatePointsOfContention : IEquatable<PossibleCandidatePointsOfContention>
        {
            /// <summary>
            /// The collection of one possibility of points of contention for a given speech candidate.
            /// </summary>
            public readonly ICollection<SpeechPointOfContention> pointsOfContention;

            /// <summary>
            /// The collection of possibilities for points of contention for other speech candidates for the same speech segment that contain at least one
            /// point of contention that matches one or more of this candidate's possible points of contention.
            /// </summary>
            public readonly ICollection<PossibleCandidatePointsOfContention> linkedCandidatePOCs;

            private int MatchingPOCCountInLinkedCandidates;

            public PossibleCandidatePointsOfContention(ICollection<SpeechPointOfContention> pointsOfContention)
            {
                this.pointsOfContention = pointsOfContention;
                linkedCandidatePOCs = new List<PossibleCandidatePointsOfContention>();
                MatchingPOCCountInLinkedCandidates = 0;
            }

            /// <summary>
            /// Get the number of points of contention from the given candidate that matches one or more of this candidate's points of contention.
            /// </summary>
            /// <param name="candidatePointOfContention">The given candidate to compare this candidate to.</param>
            /// <returns>The number of points of contention matching between the given candidate and this candidate.</returns>
            public int GetMatchingPointsOfContentionCount(PossibleCandidatePointsOfContention candidatePointOfContention) => pointsOfContention.Intersect(candidatePointOfContention.pointsOfContention).Count();

            /// <summary>
            /// Link the given possible candidate points of contention to this one.
            /// </summary>
            /// <param name="candidatePointOfContention">The given candidate points of contention to link if there's a matching point of contention in it.</param>
            public void LinkCandidate(PossibleCandidatePointsOfContention candidatePointOfContention)
            {
                MatchingPOCCountInLinkedCandidates += GetMatchingPointsOfContentionCount(candidatePointOfContention);
                linkedCandidatePOCs.Add(candidatePointOfContention);
            }

            /// <summary>
            /// Returns the number of points of contention in the candidates linked to this candidate matching the points of contention in this candidate.
            /// </summary>
            /// <returns>The number of points of contention in the candidates linked to this candidate matching the points of contention in this candidate.</returns>
            public int GetMatchingPointsOfContentionInLinkedCandidates() => MatchingPOCCountInLinkedCandidates;

            public static bool operator ==(PossibleCandidatePointsOfContention candidatePOC1, PossibleCandidatePointsOfContention candidatePOC2)
            {
                if (ReferenceEquals(candidatePOC1, candidatePOC2)) return true;
                if (ReferenceEquals(candidatePOC1, null)) return false;
                if (ReferenceEquals(candidatePOC2, null)) return false;

                return candidatePOC1.Equals(candidatePOC2);
            }

            public static bool operator !=(PossibleCandidatePointsOfContention candidatePOC1, PossibleCandidatePointsOfContention candidatePOC2)
            {
                if (ReferenceEquals(candidatePOC1, candidatePOC2)) return false;
                if (ReferenceEquals(candidatePOC1, null)) return true;
                if (ReferenceEquals(candidatePOC2, null)) return true;

                return !candidatePOC1.Equals(candidatePOC2);
            }

            public bool Equals([AllowNull] PossibleCandidatePointsOfContention other) => pointsOfContention.SequenceEqual(other.pointsOfContention);

            public override int GetHashCode()
            {
                var hash = (pointsOfContention.Count + linkedCandidatePOCs.Count) * 17;
                foreach (var pointOfContention in pointsOfContention)
                {
                    hash =  hash * 17 + pointOfContention.GetHashCode();
                }
                
                return hash;
            }

            public override bool Equals(object obj)
            {
                PossibleCandidatePointsOfContention candidatePOC = obj as PossibleCandidatePointsOfContention;
                return candidatePOC != null && Equals(candidatePOC);
            }
        }
    }    
}
