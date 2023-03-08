using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Service;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.TTSPreProcessing
{
    public class PreProcessTTS: IPreProcessTTS
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;
        private ISpeechSynthesizer speechSynthesizer;

        public PreProcessTTS(IOrchestratorLogger<TestingFrameworkOrchestrator> logger)
        {
            this.logger = logger;
        }

        public PreProcessTTS(IOrchestratorLogger<TestingFrameworkOrchestrator> logger, ISpeechSynthesizer speechSynthesizer)
        {
            this.logger = logger;
            this.speechSynthesizer = speechSynthesizer;
        }

        public string DoTTSPreProcessing(List<PreProcessTTSInput> inputs)
        {
            if(this.speechSynthesizer == null)
            {
                this.speechSynthesizer = new SpeechSynthesizerDefault(inputs[0].PreProcessingStepConfig.ServiceConfiguration.SubscriptionKey, inputs[0].PreProcessingStepConfig.ServiceConfiguration.Region, this.logger);
            }

            TranslationMappingMethodEnum translationMappingMethod = inputs[0].PreProcessingStepConfig.TranslationMappingMethod;

            List<PreProcessTTSInput> mappedSegments;

            double maxTargetRate = inputs[0].PreProcessingStepConfig.MaxSpeechRate;
            double minTargetRate = inputs[0].PreProcessingStepConfig.MinSpeechRate;

            if (minTargetRate > maxTargetRate) {
                logger.LogError($"MinTargetRate defined in config ({minTargetRate}) is higher than MaxTargetRate ({maxTargetRate})");
                throw new ArgumentException($"MinTargetRate defined in config ({minTargetRate}) is higher than MaxTargetRate ({maxTargetRate})");
            }


            switch (translationMappingMethod)
            {
                case TranslationMappingMethodEnum.StandardScaleAndFit:
                    mappedSegments = StandardScaleAndFit(inputs);
                    break;
                case TranslationMappingMethodEnum.CompensatePauses_AnchorMiddle:
                    mappedSegments = CompensatePausesAnchorMiddle(inputs);
                    break;
                case TranslationMappingMethodEnum.CompensatePauses_AnchorStart:
                    mappedSegments = CompensatePausesAnchorStart(inputs);
                    break;
                default:
                    mappedSegments = StandardScaleAndFit(inputs);
                    break;
            }

            string outputSSML = GenerateSSMLText(mappedSegments);
            return outputSSML;
        }


        private List<PreProcessTTSInput> StandardScaleAndFit(List<PreProcessTTSInput> inputs)
        {
            foreach (var input in inputs)
            {
                ValidateInput(input);
                double rate = PreprocessTTSHelper.GetScaledRateAsync(input, this.speechSynthesizer, logger);
                input.Rate = rate;
            }

            return inputs;
        }

        private List<PreProcessTTSInput> CompensatePausesAnchorMiddle(List<PreProcessTTSInput> inputs)
        {
            logger.LogInformation($"Performing Text to Speech Preprocessing on {inputs.Count} segments.");
            foreach (var input in inputs)
            {
                ValidateInput(input);
                double rate = PreprocessTTSHelper.GetRelativeTargetRate(input, logger);
                input.Rate = rate;
            }

            TimeSpan medianPause = PreprocessTTSHelper.CalculateMedianPause(inputs);
            TimeSpan nominalPause = TimeSpan.FromMilliseconds(250); // Setting nominal pause as average spoken duration of a english word

            string targetLanguage = inputs[0].TargetLocale.Split('-')[0];
            if (SpeechRateLookup.Rate.ContainsKey(targetLanguage))
            {
                nominalPause = new TimeSpan(0, 0, 0, 0, (int)(60.0 / SpeechRateLookup.Rate[targetLanguage].WordRate * 1000));
            }

            TimeSpan previous_target_offset = new TimeSpan(0);
            TimeSpan previous_target_duration = new TimeSpan(0);

            TimeSpan previous_source_offset = new TimeSpan(0);
            TimeSpan previous_source_duration = new TimeSpan(0);


            foreach (PreProcessTTSInput source_segment in inputs)
            {
                var source_duration = source_segment.Duration;
                var source_offset = source_segment.Offset;

                var target_duration = PreprocessTTSHelper.GetTargetDuration(source_segment, this.speechSynthesizer);
                var target_offset = source_offset - (target_duration - source_duration) / 2;

                if (target_offset.TotalMilliseconds < 0)
                {
                    target_offset = new TimeSpan(0);
                }

                if (target_offset == previous_target_offset + previous_target_duration && source_segment.SegmentID != 1)
                {
                    target_offset += nominalPause;
                    source_segment.HumanInterventionRequired = true;
                    source_segment.HumanInterventionReason = "After translation this segment just fit in the space available, a small pause was added before it to space it out.";
                }
                else if (target_offset < previous_target_offset + previous_target_duration)
                {
                    source_segment.HumanInterventionRequired = true;
                    var previous_pause = source_segment.Offset - (previous_source_offset + previous_source_duration);
                    if (previous_pause < medianPause)
                    {
                        target_offset = previous_target_offset + previous_target_duration + nominalPause;
                        source_segment.HumanInterventionReason = "After translation this segment overlapped with previous segment, but since the pause before this segment in the source was relatively small, this segment was NOT sped up and placed right after the previous segment with a small pause";
                    }
                    else
                    {
                        var target_rate = source_segment.Rate * ((previous_target_offset + previous_target_duration - target_offset) + target_duration + nominalPause) / target_duration;
                        source_segment.Rate = target_rate;
                        target_duration = PreprocessTTSHelper.GetTargetDuration(source_segment, this.speechSynthesizer);
                        target_offset = previous_target_offset + previous_target_duration + nominalPause;
                        source_segment.HumanInterventionReason = "After translation this segment overlapped with previous segment, also the pause for this segment in the source was relatively large (and yet the translated segment did not fit), hence this segment was sped up and placed after the previous segment with a small pause";
                    }
                }

                source_segment.Offset = target_offset;
                source_segment.Duration = target_duration;

                previous_target_offset = target_offset;
                previous_target_duration = target_duration;

                previous_source_offset = source_offset;
                previous_source_duration = source_duration;
            }

            return inputs;
        }
         
        private List<PreProcessTTSInput> CompensatePausesAnchorStart(List<PreProcessTTSInput> inputs)
        {
            logger.LogInformation($"Performing Text to Speech Preprocessing on {inputs.Count} segments.");
            foreach (var input in inputs)
            {
                ValidateInput(input);
                double rate = PreprocessTTSHelper.GetRelativeTargetRate(input, logger);
                input.Rate = rate;
            }

            TimeSpan nominalPause = new TimeSpan(0, 0, 0, 0, 250); // Setting nominal pause as average spoken duration of a english word

            string targetLanguage = inputs[0].TargetLocale.Split('-')[0];
            if (SpeechRateLookup.Rate.ContainsKey(targetLanguage))
            {
                nominalPause = new TimeSpan(0, 0, 0, 0, (int)(60.0 / SpeechRateLookup.Rate[targetLanguage].WordRate * 1000));
            }

            for(int segment_index = 0; segment_index < inputs.Count - 1; segment_index++)
            {

                var target_duration = PreprocessTTSHelper.GetTargetDuration(inputs[segment_index], this.speechSynthesizer);
                var target_offset = inputs[segment_index].Offset;

                if (target_offset + target_duration > inputs[segment_index + 1].Offset - nominalPause)
                {
                    var target_rate = inputs[segment_index].Rate * ((target_duration.TotalMilliseconds) / ((inputs[segment_index + 1].Offset.TotalMilliseconds - inputs[segment_index].Offset.TotalMilliseconds) - nominalPause.TotalMilliseconds));
                    inputs[segment_index].Rate = target_rate;
                    target_duration = PreprocessTTSHelper.GetTargetDuration(inputs[segment_index], this.speechSynthesizer);
                }

                inputs[segment_index].Duration = target_duration;
            }

            return inputs;
        }

        private string GenerateSSMLText(List<PreProcessTTSInput> inputs)
        {
            string resultSsml = $"<speak version = \"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" xmlns:mstts=\"https://www.w3.org/2001/mstts\" xml:lang=\"en-US\">";

            TimeSpan previousEndingOffset = TimeSpan.Zero;

            for (int i = 0; i < inputs.Count; i++) 
            {
                var input = inputs[i];
                resultSsml += $"<voice name = \"{input.VoiceInfo.VoiceName}\" >";

                TimeSpan pause = input.Offset - previousEndingOffset;
                previousEndingOffset = input.Offset + input.Duration;

                resultSsml += $"<mstts:silence  type=\"Tailing\" value=\"0ms\"/>";
                resultSsml += $"<mstts:silence  type=\"Leading\" value=\"0ms\"/>";
                resultSsml += $"<break time=\"{pause.TotalMilliseconds}ms\"/>";

                input.SpeakingStyle = null;
                if (input.Rate != 0)
                {
                    resultSsml += $"<prosody rate = \"{input.Rate}\">";
                }
                if (input.SpeakingStyle != null)
                {
                    resultSsml += $"<mstts:express-as style=\"{input.SpeakingStyle}\">";
                    logger.LogInformation($" {nameof(input.SpeakingStyle)} is null or empty.");
                }
                resultSsml += input.TranslatedText;
                if (input.SpeakingStyle != null)
                {
                    resultSsml += " </mstts:express-as>";
                }
                if (input.Rate != 0)
                {
                    resultSsml += "</prosody>";
                }

                if (input.HumanInterventionRequired)
                {
                    resultSsml += $"<!-- Warning: Intervention Requied - {input.HumanInterventionReason} -->";
                }

                resultSsml += "</voice>";

            }

            resultSsml += "</speak>";

            return resultSsml;
        }

        private void ValidateInput(PreProcessTTSInput input)
        {
            if (string.IsNullOrEmpty(input.TargetLocale))
            {
                logger.LogError($"Argument {nameof(input.TargetLocale)} is null or Empty");
                throw new ArgumentNullException(nameof(input.TargetLocale));
            }

            if (string.IsNullOrEmpty(input.TranslatedText))
            {
                logger.LogError($"Argument {nameof(input.TranslatedText)} is null or Empty");
                throw new ArgumentNullException(nameof(input.TranslatedText));
            }
        }

    }
}
