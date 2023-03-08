//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Utilities.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.ExecutionPipeline.Execution
{
    public interface IExecutePipelineStep
    {
        Task<List<string>> WriteToBlobStoreAsync(BlobStorageInput storageInput);

        Task<byte[]> ReadBinaryFileAsync(string filePath);

        Task<List<byte[]>> ReadBinaryFilesAsync(List<string> filePaths);
    }
}
