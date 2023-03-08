//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using AIPlatform.TestingFramework.Utilities.Diagnostics;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.Utilities.Storage
{
    public class BlobStorageManager : IStorageManager
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;
        private readonly BlobContainerClient blobContainerClient;

        public BlobStorageManager(IOrchestratorLogger<TestingFrameworkOrchestrator> logger, BlobContainerClient blobContainerClient)
        {
            this.logger = logger;
            this.blobContainerClient = blobContainerClient;
        }

        public async Task<string> ReadTextFileAsync(string filePath)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Dictionary<string, double> metrics = new Dictionary<string, double>();

            BlobClient blobClient = blobContainerClient.GetBlobClient(filePath);

            BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync();
            string downloadedData = downloadResult.Content.ToString();

            metrics.Add($"ReadTextFileMS", stopwatch.ElapsedMilliseconds);

            stopwatch.Stop();

            logger.LogEvent("ReadTextFileMS", null, metrics, true);

            logger.LogInformation($"{downloadedData.Length} characters were read from file: {filePath}");
            
            return downloadedData;

        }

        public async Task<byte[]> ReadBinaryFileAsync(string filePath)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Dictionary<string, double> metrics = new Dictionary<string, double>();

            BlobClient blobClient = blobContainerClient.GetBlobClient(filePath);

            using MemoryStream memoryStream = new MemoryStream();
            Response response = await blobClient.DownloadToAsync(memoryStream);
            if (response.IsError)
            {
                string message = $"Error reading binary file: {filePath}. Response status: {response.Status}, Response Reason: {response.ReasonPhrase}";
                logger.LogError(message);
                throw new Exception(message);
            }

            //TODO: Need to handle large files (len > max int)
            int len = (int)memoryStream.Length;
            byte[] fileData = new byte[len];
            memoryStream.Position = 0;
            memoryStream.Read(fileData, 0, len);

            metrics.Add($"ReadBinaryFileMS", stopwatch.ElapsedMilliseconds);

            stopwatch.Stop();

            logger.LogEvent("ReadBinaryFileMS", null, metrics, true);

            logger.LogInformation($"{len} bytes were read from file: {filePath}");

            return fileData;
        }

        public async Task<List<byte[]>> ReadBinaryFilesAsync(List<string> fileNames)
        {
            Dictionary<string, double> metrics = new Dictionary<string, double>();

            List<byte[]> binaryFiles = new List<byte[]>();
            foreach (string filePath in fileNames)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                byte[] binaryData = await ReadBinaryFileAsync(filePath);

                var duration = stopwatch.ElapsedMilliseconds;
                metrics.Add(filePath, duration);

                binaryFiles.Add(binaryData);
            }

            logger.LogEvent("ReadBinaryFilesMS", null, metrics, true);
            return binaryFiles;
        }

        public async Task<string> WriteTextFileAsync(string fileContent, string filePath, bool overWrite = true)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            Dictionary<string, double> metrics = new Dictionary<string, double>();

            BlobClient blobClient = blobContainerClient.GetBlobClient(filePath);

            BinaryData binaryData = new BinaryData(fileContent);
            
            Response<BlobContentInfo> response = await blobClient.UploadAsync(binaryData, overWrite, CancellationToken.None);

            var duration = stopwatch.ElapsedMilliseconds;
            stopwatch.Stop();

            metrics.Add(filePath, duration);
            if (response.GetRawResponse().Status == 201)
            {
                logger.LogInformation($"File uploaded. BlobUri: {blobClient.Uri.AbsoluteUri}. Duration={duration}ms");
                return blobClient.Uri.AbsoluteUri;
            }
            else
            {
                string message = $"Error occured uploading fileName {filePath} to Blob store. Error details; {response.GetRawResponse()}";
                
                logger.LogError(message);

                throw new Exception(message);
            }
        }

        public async Task<List<string>> WriteFilesToStorageAsync(BlobStorageInput blobStorageInput)
        {
            string folderPath = blobStorageInput.StorageConfiguration.FolderPath;
            bool overWrite = blobStorageInput.StorageConfiguration.OverWriteFile;

            List<string> uploadedBlobs = new List<string>();
            int idx = 0;

            //writing binary files only and setting the type to .wav
            //TODO need to do something similar for textfiles
            Dictionary<string, double> metrics = new Dictionary<string, double>();
            foreach (byte[] binaryFile in blobStorageInput.BinaryFiles)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();

                string fileName = $"{folderPath}/tts_{idx}.wav";
                BlobClient blobClient = blobContainerClient.GetBlobClient(fileName);

                BinaryData binaryData = new BinaryData(binaryFile);
                Response<BlobContentInfo> response = await blobClient.UploadAsync(binaryData, overWrite, CancellationToken.None);

                var duration = stopwatch.ElapsedMilliseconds;
                metrics.Add(fileName, duration);
                if (response.GetRawResponse().Status == 201)
                {
                    uploadedBlobs.Add(blobClient.Uri.AbsoluteUri);
                    idx++;
                    logger.LogInformation($"File uploaded. BlobUri: {blobClient.Uri.AbsoluteUri}. Duration={duration}ms");
                }
                else
                {
                    logger.LogError($"Error occured uploading fileName to Blob store. Error details; {response.GetRawResponse()}");
                }

                stopwatch.Stop();
            }

            logger.LogEvent("WriteFilesToStorageAsyncInMS", null, metrics, true);
            return uploadedBlobs;
        }
    }
}
