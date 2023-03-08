//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.ExecutionPipeline.Execution
{
    public abstract class ExecutePipelineStep : IExecutePipelineStep
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;
        private readonly IStorageManager storageManager;

        public ExecutePipelineStep(IOrchestratorLogger<TestingFrameworkOrchestrator> logger, IStorageManager storageManager)
        {
            this.logger = logger;
            this.storageManager = storageManager;
        }

        public async Task<List<string>> WriteToBlobStoreAsync(BlobStorageInput storageInput)
        {
            ValidateInput(storageInput);
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<string> result = await storageManager.WriteFilesToStorageAsync(storageInput);

            var duration = stopwatch.ElapsedMilliseconds;

            logger.LogInformation($"WriteToBlobStore Duration={duration}ms");

            return result;
        }

        public async Task<byte[]> ReadBinaryFileAsync(string filePath)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            byte[] result = await storageManager.ReadBinaryFileAsync(filePath);

            var duration = stopwatch.ElapsedMilliseconds;

            logger.LogInformation($"{nameof(MethodBase.Name)} Duration={duration}ms");

            return result;
        }

        public async Task<List<byte[]>> ReadBinaryFilesAsync(List<string> filePaths)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<byte[]> result = await storageManager.ReadBinaryFilesAsync(filePaths);

            var duration = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            logger.LogInformation($"Executed ReadBinaryFilesAsync Duration={duration}ms");

            return result;
        }

        private void ValidateInput(BlobStorageInput bsWriterInput)
        {
            if (bsWriterInput == null)
            {
                throw new ArgumentNullException(nameof(bsWriterInput));
            }

            if(bsWriterInput.StorageConfiguration == null)
            {
                throw new ArgumentNullException(nameof(bsWriterInput.StorageConfiguration));
            }

            if ((bsWriterInput.BinaryFiles.Count == 0) && (bsWriterInput.TextFiles.Count == 0))
            {
                throw new ArgumentException("Either BinaryFiles or TextFiles should have at least 1 file to write to blob storage");
            }

            if (string.IsNullOrEmpty(bsWriterInput.StorageConfiguration.FolderPath))
            {
                throw new ArgumentException("String argument is null or empty", nameof(bsWriterInput.StorageConfiguration.FolderPath));
            }
        }
    }
}
