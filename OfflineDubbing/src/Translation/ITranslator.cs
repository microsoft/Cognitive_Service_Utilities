//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.Translation
{
    public interface ITranslator
    {
        Task<string> DoTranslation(TranslatorInput input);
    }
}
