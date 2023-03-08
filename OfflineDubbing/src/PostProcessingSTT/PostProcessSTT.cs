using AIPlatform.TestingFramework.Common;
using AIPlatform.TestingFramework.STT;
using AIPlatform.TestingFramework.Translation;
using AIPlatform.TestingFramework.TTSPreProcessing;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace AIPlatform.TestingFramework.PostProcessingSTT
{
    public class PostProcessSTT : IPostProcessSTT
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;
        private static int TRANSLATION_CHAR_LIMIT = 50_000;

        public PostProcessSTT(IOrchestratorLogger<TestingFrameworkOrchestrator> logger)
        {
            this.logger = logger;
        }

        public string DoSpeechToTextPostProcessing(PostProcessSTTInput input)
        {
            logger.LogInformation($"Performing post-processing.");

            ICollection<SpeechOutputSegment> combinedSegments;
            if (input.PostProcessingSTTStepConfiguration.ConcatenateMatchingSegments)
            {
                // Combine segments by language, speaker ID and emotion
                combinedSegments = CombineSegments(input.Input);
            }
            else
            {
                combinedSegments = input.Input;
            }

            // Prepare input for translation module
            ICollection<TranslatorInputSegment> translationInput = GenerateTranslationInput(combinedSegments, input.PostProcessingSTTStepConfiguration);
            logger.LogInformation($"Generated input for translation.");

            // Prepare inputs for Text-to-Speech preprocessing and evaluation module
            ICollection<PreProcessTTSInputSegment> preProcessTTSInput = GenerateTTSInput(combinedSegments, translationInput);
            logger.LogInformation($"Generated input for text-to-speech preprocessing.");

            var ouput = new List<object>
            {
                translationInput,
                preProcessTTSInput,
                combinedSegments
            };

            return JsonConvert.SerializeObject(ouput);
        }

        private ICollection<SpeechOutputSegment> CombineSegments(ICollection<SpeechOutputSegment> input)
        {
            var combinedSegments = new List<SpeechOutputSegment>();
            SpeechOutputSegment previousSegment = null;
            SpeechOutputSegment newSegment = null;
            var segmentId = 0;
            var charCount = 0;

            foreach (var segment in input)
            {
                var segmentCharCount = segment.DisplayText.Length;
                if (previousSegment != null)
                {
                    if (previousSegment.IdentifiedLocale == segment.IdentifiedLocale &&
                        previousSegment.IdentifiedSpeaker == segment.IdentifiedSpeaker &&
                        previousSegment.IdentifiedEmotion == segment.IdentifiedEmotion &&
                        charCount + segmentCharCount <= TRANSLATION_CHAR_LIMIT)
                    {
                        newSegment.Append(segment);
                        charCount += segmentCharCount;
                    }
                    else
                    {
                        combinedSegments.Add(newSegment);
                        segmentId++;
                        newSegment = segment.Copy(segmentId);
                        charCount = segmentCharCount;
                    }
                }
                else
                {
                    newSegment = segment.Copy(segmentId);
                    charCount = segmentCharCount;
                }

                previousSegment = segment;
            }

            if (newSegment != null)
            {
                combinedSegments.Add(newSegment);
            }

            return combinedSegments;
        }

        private ICollection<TranslatorInputSegment> GenerateTranslationInput(ICollection<SpeechOutputSegment> input, PostProcessSTTConfiguration config)
        {
            var translationInput = new List<TranslatorInputSegment>();

            foreach (var speechToTextOutputSegment in input)
            {
                TranslatorInputSegment translatorInputSegment;

                if (config.SourceLocales.Contains(speechToTextOutputSegment.IdentifiedLocale))
                {
                    // Translate segment using the expected locale as source locale
                    translatorInputSegment = new TranslatorInputSegment(
                        speechToTextOutputSegment.DisplayText,
                        speechToTextOutputSegment.IdentifiedLocale,
                        config.TargetLocale,
                        speechToTextOutputSegment.SegmentID);
                }
                else
                {
                    if (config.IgnoreUnexpectedSourceLocales)
                    {
                        // Leave segment in its original locale
                        translatorInputSegment = new TranslatorInputSegment(
                            speechToTextOutputSegment.DisplayText,
                            speechToTextOutputSegment.IdentifiedLocale,
                            speechToTextOutputSegment.IdentifiedLocale,
                            speechToTextOutputSegment.SegmentID);
                    }
                    else
                    {
                        // Translate unexpected locale
                        translatorInputSegment = new TranslatorInputSegment(
                            speechToTextOutputSegment.DisplayText,
                            speechToTextOutputSegment.IdentifiedLocale,
                            config.TargetLocale,
                            speechToTextOutputSegment.SegmentID);
                    }
                }

                translationInput.Add(translatorInputSegment);
            }

            return translationInput;
        }

        private ICollection<PreProcessTTSInputSegment> GenerateTTSInput(ICollection<SpeechOutputSegment> input, ICollection<TranslatorInputSegment> translatorInput)
        {
            var preProcessTTSInput = new List<PreProcessTTSInputSegment>();

            for (int i = 0; i < input.Count; i++)
            {
                var speechToTextOutputSegment = input.ElementAt(i);

                ICollection<PreProcessTTSInputSegment.TimeStamp> timeStamps = new List<PreProcessTTSInputSegment.TimeStamp>();

                foreach (var timeStamp in speechToTextOutputSegment.TimeStamps) 
                {
                    timeStamps.Add(new PreProcessTTSInputSegment.TimeStamp()
                    {
                        Word = timeStamp.Word,
                        Duration = timeStamp.Duration,
                        Offset = timeStamp.Offset
                    });
                }

                var preProcessTTSInputSegment = new PreProcessTTSInputSegment()
                {
                    LexicalText = speechToTextOutputSegment.LexicalText,
                    DisplayText = speechToTextOutputSegment.DisplayText,
                    IdentifiedEmotion = speechToTextOutputSegment.IdentifiedEmotion,
                    IdentifiedLocale = speechToTextOutputSegment.IdentifiedLocale,
                    TargetLocale = translatorInput.ElementAt(i).TargetLocale,
                    IdentifiedSpeaker = speechToTextOutputSegment.IdentifiedSpeaker,
                    Duration = speechToTextOutputSegment.Duration,
                    Offset = speechToTextOutputSegment.Offset,
                    SegmentID = speechToTextOutputSegment.SegmentID,
                    TimeStamps = timeStamps
                };

                preProcessTTSInput.Add(preProcessTTSInputSegment);
            }

            return preProcessTTSInput;
        }
    }
}
