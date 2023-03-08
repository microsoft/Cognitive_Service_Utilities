//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Evaluation.Translation;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.Evaluation.Interfaces
{
    public interface ITranslationCorrectnessEvaluator
    {
        /// <summary>
        /// Evaluates the correctness of each of the given translation output segments according to the given threshold and determine whether
        /// human intervention is needed in each segment.
        /// </summary>
        /// <param name="input">Translation correctness evluation module input.</param>
        /// <returns>Serialized collection of translation correctness segments.</returns>
        public Task<string> EvaluateCorrectnessAsync(TranslationCorrectnessInput input);
    }
}
