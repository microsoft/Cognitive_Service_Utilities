//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.Utilities.Storage
{
    public interface IStorageManager
    {
        Task<string> WriteTextFileAsync(string fileContent, string filePath, bool overWrite = true);

        Task<List<string>> WriteFilesToStorageAsync(BlobStorageInput blobStorageInput);

        Task<string> ReadTextFileAsync(string filePath);

        Task<byte[]> ReadBinaryFileAsync(string filePath);

        Task<List<byte[]>> ReadBinaryFilesAsync(List<string> filePaths);
    }
}
