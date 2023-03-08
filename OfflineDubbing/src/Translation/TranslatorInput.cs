using AIPlatform.TestingFramework.Utilities.Diagnostics.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace AIPlatform.TestingFramework.Translation
{
    public class TranslatorInput
    {
        public TranslatorConfiguration TranslatorStepConfiguration { get; set; }

        public ICollection<TranslatorInputSegment> Input { get; set; }

        /// <summary>
        /// Constructor for Translator module input.
        /// </summary>
        /// <param name="config">Configuration settings for the translation step.</param>
        /// <param name="input">Segments of text input to translate.</param>
        public TranslatorInput(TranslatorConfiguration config, ICollection<TranslatorInputSegment> input)
        {
            TranslatorStepConfiguration = config;
            Input = input;
        }

        override
        public string ToString()
        {
            var str = $"{Environment.NewLine}{{{Environment.NewLine}\tTranslatorStepConfiguration: " +
                $"{TranslatorStepConfiguration.ToString().Indent()}," +
                $"{Environment.NewLine}\tInput:{Input.ToJSONArray().Indent()}{Environment.NewLine}}}";

            return str;
        }
    }
}
