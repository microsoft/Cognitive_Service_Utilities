using AIPlatform.TestingFramework;
using AIPlatform.TestingFramework.ExecutionPipeline.Execution;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace AIPlatform.EvaluationFramework.Test.Unit
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class PipelineTest
    {
        MockExecutePipelineStep dummyPipelineStep;
        [TestInitialize]
        public void Initialize()
        {
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            var blobWriterMock = new Mock<IStorageManager>();
            dummyPipelineStep = new MockExecutePipelineStep(loggerMock.Object, blobWriterMock.Object);
        }

        [TestMethod]
        public async Task WriteToBlobStoreAsync_Throws_ArgumentException_When_BlobStorageInput_IsNull()
        {
            string expectedErrorMessage = "Value cannot be null. (Parameter 'bsWriterInput')";
            string actualErrorMessage = string.Empty;

            try
            {
                await dummyPipelineStep.WriteToBlobStoreAsync(null);
            }
            catch (ArgumentNullException ex)
            {
                actualErrorMessage = ex.Message;
            }

            Assert.AreEqual(expectedErrorMessage, actualErrorMessage);
        }

        [TestMethod]
        public async Task WriteToBlobStoreAsync_Throws_ArgumentException_When_BlobStorageConfiguration_IsNull()
        {
            BlobStorageInput bswInput = new BlobStorageInput(null, new List<byte[]>());

            string expectedErrorMessage = "Value cannot be null. (Parameter 'StorageConfiguration')";
            string actualErrorMessage = string.Empty;
            try
            {
                await dummyPipelineStep.WriteToBlobStoreAsync(bswInput);
            }
            catch (ArgumentNullException ex)
            {
                actualErrorMessage = ex.Message;
            }

            Assert.AreEqual(expectedErrorMessage, actualErrorMessage);
        }

        [TestMethod]
        public async Task WriteToBlobStoreAsync_Throws_ArgumentException_When_BlobStorageConfiguration_FolderPath_IsNullOrEmpty()
        {
            BlobStorageConfiguration bswConfig = new BlobStorageConfiguration();
            bswConfig.FolderPath = "";
            byte[] binFile = new byte[1024];
            List<byte[]> binaryFiles = new List<byte[]>
            {
                binFile
            };

            BlobStorageInput bswInput = new BlobStorageInput(bswConfig, binaryFiles);

            string expectedErrorMessage = "String argument is null or empty (Parameter 'FolderPath')";
            string actualErrorMessage = string.Empty;
            try
            {
                await dummyPipelineStep.WriteToBlobStoreAsync(bswInput);
            }
            catch (ArgumentException ex)
            {
                actualErrorMessage = ex.Message;
            }

            Assert.AreEqual(expectedErrorMessage, actualErrorMessage);
        }

        [TestMethod]
        public async Task WriteToBlobStoreAsync_Throws_ArgumentException_When_BlobStorageConfiguration_Doesnt_Have_Files_ToWrite()
        {
            BlobStorageConfiguration bswConfig = new BlobStorageConfiguration();
            bswConfig.FolderPath = ".";
            BlobStorageInput bswInput = new BlobStorageInput(bswConfig, new List<byte[]>());

            string expectedErrorMessage = "Either BinaryFiles or TextFiles should have at least 1 file to write to blob storage";
            string actualErrorMessage = string.Empty;
            try
            {
                await dummyPipelineStep.WriteToBlobStoreAsync(bswInput);
            }
            catch (ArgumentException ex)
            {
                actualErrorMessage = ex.Message;
            }

            Assert.AreEqual(expectedErrorMessage, actualErrorMessage);
        }

        [TestMethod]
        public async Task WriteToBlobStoreAsync_Succeeds()
        {
            var loggerMock = new Mock<IOrchestratorLogger<TestingFrameworkOrchestrator>>();
            
            var blobWriterMock = new Mock<IStorageManager>();
            blobWriterMock.Setup(m => m.WriteFilesToStorageAsync(It.IsAny<BlobStorageInput>())).ReturnsAsync(new List<string> { "filePath" });

            dummyPipelineStep = new MockExecutePipelineStep(loggerMock.Object, blobWriterMock.Object);

            BlobStorageConfiguration bswConfig = new BlobStorageConfiguration
            {
                FolderPath = "."
            };
            BlobStorageInput bswInput = new BlobStorageInput(bswConfig, new List<byte[]> { new byte[1024] });

            var result = await dummyPipelineStep.WriteToBlobStoreAsync(bswInput);

            //assert that the method was called once
            blobWriterMock.Verify(m => m.WriteFilesToStorageAsync(bswInput), Times.Once);
        }
    }

    [ExcludeFromCodeCoverage]
    public class MockExecutePipelineStep : ExecutePipelineStep
    {
        public MockExecutePipelineStep(IOrchestratorLogger<TestingFrameworkOrchestrator> logger, IStorageManager storageWriter) : 
            base(logger, storageWriter)
        {
        }
    }
}
