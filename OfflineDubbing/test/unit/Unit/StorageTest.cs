using AIPlatform.TestingFramework;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Azure;
using Azure.Core;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AIPlatform.EvaluationFramework.Test.Unit
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class StorageTest
    {
        BlobStorageManager sm;
        Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>> loggerMock;
        const string Account_Url = "https://testaccouunt.blob.core.windows.net/";
        const string Container_Name = "testContainer";
        const string File_Name = "tts_0.wav";

        private void SetupMocks(BlobStorageInput storageInput)
        {
            string fileUri = $"{Account_Url}{Container_Name}/{storageInput.StorageConfiguration.FolderPath}/{File_Name}";

            loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            
            BlobContentInfo bcInfo = BlobsModelFactory.BlobContentInfo(new ETag(), DateTimeOffset.Now, null, "", "", "", 0);
            Response<BlobContentInfo> response = Response.FromValue(bcInfo, new TestResponse());

            Mock<BlobClient> blobClientMock = new Mock<BlobClient>();
            blobClientMock.Setup(m => m.Uri)
                .Returns(new Uri(fileUri));
                       
            blobClientMock.Setup(m => m.UploadAsync(It.IsAny<BinaryData>(),true, default(CancellationToken)))
                 .ReturnsAsync(response);

            Mock<BlobContainerClient> blobContainerClientMock = new Mock<BlobContainerClient>();
            blobContainerClientMock.Setup(m => m.GetBlobClient(It.IsAny<string>()))
                .Returns(blobClientMock.Object);

            sm = new BlobStorageManager(loggerMock.Object, blobContainerClientMock.Object);
        }

        [TestMethod]
        public async Task WriteFilesToStorageAsync_Returns_Same_Count_Of_Files_As_Number_Of_FilesAsync()
        {
            BlobStorageConfiguration bsWriterConfig = new BlobStorageConfiguration
            {
                FolderPath = "test"
            };
            List<byte[]> binaryFiles = new List<byte[]>
            {
                new byte[1024],
            };

            //write 1 binary file
            BlobStorageInput bsWriterInput = new BlobStorageInput(bsWriterConfig, binaryFiles);

            //setup all the mocks
            SetupMocks(bsWriterInput);

            //execute test
            var result = await sm.WriteFilesToStorageAsync(bsWriterInput);

            //assert 1 output
            Assert.AreEqual(1, result.Count);

            //assert fileUriFormat is same
            string expectedFileUri = $"{Account_Url}{Container_Name}/{bsWriterConfig.FolderPath}/{File_Name}".ToLower();
            Assert.AreEqual(expectedFileUri, result[0].ToLower());

            //assert logMetric was called once in the logger
            loggerMock.Verify(m => m.LogEvent(It.IsAny<string>(), It.IsAny<Dictionary<string,string>>(), It.IsAny<Dictionary<string, double>>(), true), Times.Once());
        }
    }


    [ExcludeFromCodeCoverage]
    public class TestResponse : Response
    {
        string clientRequestId = string.Empty;
        Stream contentStream;

        public override int Status => 201; //accepted

        public override string ReasonPhrase => "Success";

        public override Stream ContentStream { get => contentStream; set => contentStream = value; }

        public override string ClientRequestId { get => clientRequestId; set => clientRequestId = value; }

        public override void Dispose()
        {
            Dispose();
            GC.SuppressFinalize(this);
        }

        protected override bool ContainsHeader(string name)
        {
            return true;
        }

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            HttpHeader header = new HttpHeader("testHeaderKey", "testHeaderValue");
            List<HttpHeader> headers = new List<HttpHeader> { header };

            return headers;
        }

        protected override bool TryGetHeader(string name, [NotNullWhen(true)] out string value)
        {
            value = "testHeaderValue";
            return true;
        }

        protected override bool TryGetHeaderValues(string name, [NotNullWhen(true)] out IEnumerable<string> values)
        {
            values = new List<string>() { "testHeaderValue" };
            return true;
        }
    }
}
