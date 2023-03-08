//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Evaluation.STT;

namespace AIPlatform.TestingFramework.Evaluation.Interfaces
{
    public interface ISpeechCorrectnessEvaluator
    {
        /// <summary>
        /// Evaluates the correctness of each of the given speech output segments according to the given thresholds and determine whether
        /// human intervention is needed in each segment.
        /// </summary>
        /// <param name="input">Speech correctness evluation module input.</param>
        /// <returns>Serialized collection of speech correctness segments.</returns>
        public string EvaluateCorrectness(SpeechCorrectnessInput input);
    }
}
