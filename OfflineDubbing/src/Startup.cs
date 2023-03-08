//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Evaluation.Interfaces;
using AIPlatform.TestingFramework.Evaluation.STT;
using AIPlatform.TestingFramework.Evaluation.Translation;
using AIPlatform.TestingFramework.PostProcessingSTT;
using AIPlatform.TestingFramework.STT;
using AIPlatform.TestingFramework.SubtitlesGeneration;
using AIPlatform.TestingFramework.Translation;
using AIPlatform.TestingFramework.TTS;
using AIPlatform.TestingFramework.TTSPreProcessing;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

[assembly: FunctionsStartup(typeof(AIPlatform.TestingFramework.Startup))]

namespace AIPlatform.TestingFramework
{
    [ExcludeFromCodeCoverage]
    public class Startup : FunctionsStartup
    {
        IConfigurationBuilder configurationBuilder;

        public override void Configure(IFunctionsHostBuilder builder)
        {
            var settings = configurationBuilder.Build();

            string blobContainerSasUrl = settings.GetValue<string>("BlobStorage_SasUri");
            BlobContainerClient blobContainerClient = new BlobContainerClient(new Uri(blobContainerSasUrl), null);

            builder.Services.AddSingleton(blobContainerClient);
            builder.Services.AddSingleton<IStorageManager, BlobStorageManager>();

            builder.Services.AddHttpClient<ISpeechToText, SpeechToText>();
            builder.Services.AddHttpClient<ITranslator, Translator>();

            builder.Services.AddSingleton(typeof(IOrchestratorLogger<>), typeof(OrchestratorLogger<>));
            builder.Services.AddSingleton<ISpeechToText, SpeechToText>();
            builder.Services.AddSingleton<IPostProcessSTT, PostProcessSTT>();
            builder.Services.AddSingleton<ISpeechCorrectnessEvaluator, SpeechCorrectnessEvaluator>();
            builder.Services.AddSingleton<ITranslator, Translator>();
            builder.Services.AddSingleton<ITranslationCorrectnessEvaluator, TranslationCorrectnessEvaluator>();
            builder.Services.AddSingleton<ITextToSpeech, TextToSpeech>();
            builder.Services.AddSingleton<IPreProcessTTS, PreProcessTTS>();
            builder.Services.AddSingleton<ISubtitlesWriting, SubtitlesWriting>();
        }

        public override void ConfigureAppConfiguration(IFunctionsConfigurationBuilder builder)
        {
            FunctionsHostBuilderContext context = builder.GetContext();

            configurationBuilder = builder.ConfigurationBuilder
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, "local.settings.json"), optional: true, reloadOnChange: true)
                .AddJsonFile(Path.Combine(context.ApplicationRootPath, $"appsettings.{context.EnvironmentName}.json"), optional: true, reloadOnChange: false)
                .AddEnvironmentVariables();
        }
    }
}
