using AIPlatform.TestingFramework.STT.TranscriptionUtils;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AIPlatform.TestingFramework.SubtitlesGeneration
{
    public class SubtitlesWriting: ISubtitlesWriting
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;

        public SubtitlesWriting(IOrchestratorLogger<TestingFrameworkOrchestrator> logger)
        {
            this.logger = logger;
        }

        public string WriteSubtitles(SubtitlesWritingConfigInput configInput)
        {
            List<WebVttSubtitle> captions = GeneratSubtitles(configInput);
            string webVttFile = GenerateWebVttFile(captions);
            logger.LogInformation($"Resulting WebVtt file: {webVttFile}");

            return webVttFile;

        }

        string GenerateWebVttFile(List<WebVttSubtitle> captions) => $"WEBVTT" + string.Join("", captions);

        List<WebVttSubtitle> GeneratSubtitles(SubtitlesWritingConfigInput configInput)
        {
            List<WebVttSubtitle> outputSubtitles = new List<WebVttSubtitle>();
            List<SubtitlesWritingInput> captionsInputs = configInput.Inputs;
            SubtitlesWritingConfiguration config = configInput.Config;
            logger.LogInformation($"Performing Subtitles Writing with the config: {config}.");

            int subtitleCount = 0;
            foreach (SubtitlesWritingInput captionsInput in captionsInputs)
            {
                int wordsAmount = CalculateWordsAmount(captionsInput.DisplayText);
                int segmentSymbolsAmount = captionsInput.DisplayText.Length;

                List<int> wordsPartitioning = FindTheCaptionsPartitioning(wordsAmount, config.NumberOfLines, config.NumberOfWordsRange);

                var wordBeginningRegEx = new Regex("\\b\\w");
                List<int> wordStartIndexes = wordBeginningRegEx.Matches(captionsInput.DisplayText).Select(x => x.Index).ToList();

                int wordsOffset = 0;
                TimeSpan timeOffset = captionsInput.Offset;
                foreach (int partition in wordsPartitioning)
                {
                    logger.LogInformation($"Generating subtitle {subtitleCount + 1} ...");
                    SubtitleText subtitleText = GenerateSubtitleText(captionsInput.DisplayText, wordStartIndexes, wordsOffset, partition, config.NumberOfLines);
                    WebVttTimestamps timestamps;
                    if (config.GenerateSubtitleLevelTimestamps)
                    {
                        int subtitleSymbolsAmount = SubtitleSymbolsAmount(subtitleText);
                        TimeSpan subtitleDuration = CalculateSubtitleDuration(subtitleSymbolsAmount, segmentSymbolsAmount, captionsInput.Duration);
                        timestamps = GenerateWebVttTimestamps(timeOffset, subtitleDuration);
                        timeOffset = timeOffset.Add(subtitleDuration);
                    } else
                    {
                        timestamps = FindTimeStamps(wordsOffset, partition, captionsInput);
                        wordsOffset += partition;
                    }
                    
                    var subtitleMetadata = ProcessHumanIntervention(captionsInput);

                    WebVttSubtitle subtitle = new WebVttSubtitle
                    {
                        SubtitleId = ++subtitleCount,
                        Timestamps = timestamps,
                        SubtitleText = subtitleText,
                        SubtitleMetadata = subtitleMetadata
                    };
                    outputSubtitles.Add(subtitle);
                }
            }

            return outputSubtitles;
        }

        public void ValidateConfiguration(SubtitlesWritingConfiguration configuration)
        {
            if (configuration == null)
            {
                logger.LogError($"Argument {nameof(configuration)} is null");
                throw new ArgumentNullException(nameof(configuration));
            }

            if (configuration.NumberOfLines <= 0)
            {
                logger.LogError($"Argument {nameof(configuration.NumberOfLines)} is 0 or negative");
                throw new ArgumentOutOfRangeException(nameof(configuration.NumberOfLines));
            }

            if (configuration.NumberOfWordsRange == null || configuration.NumberOfWordsRange.Count != 2)
            {
                logger.LogError($"Argument {configuration.NumberOfWordsRange} is null or have an incorrect size");
                throw new ArgumentOutOfRangeException(nameof(configuration.NumberOfWordsRange));
            }
        }

        int CalculateWordsAmount(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        TimeSpan CalculateSubtitleDuration(int subtitleSymbolsAmount, int segmentSymbolsAmout, TimeSpan duration) => duration * (subtitleSymbolsAmount * 1.0 / segmentSymbolsAmout);

        WebVttTimestamps GenerateWebVttTimestamps(TimeSpan offset, TimeSpan duration)
        {
            TimeSpan end = offset + duration - TimeSpan.FromMilliseconds(0.1);

            return new WebVttTimestamps(offset, end);
        }

        int SubtitleSymbolsAmount(SubtitleText subtitleText)
        {
            int count = 0;
            foreach (string line in subtitleText.Lines)
            {
                count += line.Length;
            }

            return count;
        }

        List<int> FindTheCaptionsPartitioning(int wordsAmount,int numberOfLines, List<int> wordsRangeCaption)
        {
            int wordsPerCaptionResult = -1, maxRemainder = -1;
            // check for the size
            var listOfRanges = Enumerable.Range(numberOfLines * wordsRangeCaption[0], numberOfLines * wordsRangeCaption[1]).ToList();
            foreach (int rangeValue in listOfRanges)
            {
                int remainder = wordsAmount % rangeValue;
                if (remainder == 0 || remainder >= numberOfLines * wordsRangeCaption[0])
                {
                    wordsPerCaptionResult = rangeValue;
                    maxRemainder = remainder;
                    break;
                } 
                else if (remainder > maxRemainder)
                {
                    maxRemainder = remainder;
                    wordsPerCaptionResult = rangeValue;
                }
            }

            int amount = wordsAmount / wordsPerCaptionResult;
            List<int> resultPartitioning = new List<int>();
            for (int i = 0; i < amount; ++i)
            {
                resultPartitioning.Add(wordsPerCaptionResult);
            }

            if (maxRemainder != 0)
            {
                resultPartitioning.Add(maxRemainder);
            }

            return resultPartitioning;
        }

        SubtitleText GenerateSubtitleText(string segmentText, List<int> wordStartIndexes, int wordOffset, int wordsAmount, int numberOfLines)
        {
            List<int> linesPartitioning = GenerateLinesPartitioning(wordsAmount, numberOfLines);

            List<string> captionsTextLines = new List<string>();
            for (int i = 0; i < linesPartitioning.Count; ++i)
            {
                int firstWordIndex = wordStartIndexes[wordOffset];
                int afterLastOffset = wordOffset + linesPartitioning[i];
                int afterLastWordIndex = afterLastOffset < wordStartIndexes.Count ? wordStartIndexes[afterLastOffset] : segmentText.Length;

                captionsTextLines.Add(segmentText.Substring(firstWordIndex, afterLastWordIndex - firstWordIndex).Trim());
                wordOffset += linesPartitioning[i];
            }

            SubtitleText captionText = new SubtitleText(captionsTextLines);
            return captionText;
        }

        List<int> GenerateLinesPartitioning(int wordsAmount, int lines)
        {
            List<int> linesPartitioning = new List<int>();

            int amountInLine = wordsAmount / lines;
            int remainder = wordsAmount % lines;

            for (int i = 0; i < lines; ++i)
            {
                int lineWordsAmount = amountInLine;
                if (remainder > 0)
                {
                    ++lineWordsAmount;
                    --remainder;
                }
                linesPartitioning.Add(lineWordsAmount);
            }

            return linesPartitioning;
        }

        WebVttTimestamps FindTimeStamps(int wordsOffset, int wordsAmount, SubtitlesWritingInput input)
        {
            TimeStamp[] timestamps = input.DisplayWordTimeStamps.Count != 0
                ? input.DisplayWordTimeStamps.ToArray()
                : input.TimeStamps.Count != 0
                    ? input.TimeStamps.ToArray()
                    : throw new ArgumentException("No timestamp data included in subtitle writing input.");
            
            int endIndex = wordsOffset + wordsAmount - 1;
            TimeSpan start = timestamps[wordsOffset].Offset;
            TimeSpan end = timestamps[endIndex].Offset + timestamps[endIndex].Duration;

            return new WebVttTimestamps(start, end);
        }

        SubtitleMetadata ProcessHumanIntervention(SubtitlesWritingInput captionsInput)
        {
            if (!captionsInput.HumanIntervention)
            {
                return null;
            }

            var metadata = new SubtitleMetadata
            {
                Locale = captionsInput.IdentifiedLocale,
                Speaker = captionsInput.IdentifiedSpeaker,
                HumanIntervention = captionsInput.HumanIntervention,
                HumanInterventionReasons = captionsInput.HumanInterventionReasons
            };

            return metadata;
        }
    }
}
