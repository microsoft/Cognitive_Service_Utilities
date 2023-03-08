//
// Copyright (c) 2022, Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AIPlatform.TestingFramework.Pipeline.Configuration;
using AIPlatform.TestingFramework.Utilities.Diagnostics;
using AIPlatform.TestingFramework.Utilities.Storage;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AIPlatform.TestingFramework.Utilities.Orchestrator
{
    public class OrchestratorHelper
    {
        private readonly IOrchestratorLogger<TestingFrameworkOrchestrator> logger;
        private readonly IStorageManager storageManager;

        public OrchestratorHelper(IOrchestratorLogger<TestingFrameworkOrchestrator> appLogger, IStorageManager storageManager)
        {
            logger = appLogger;
            this.storageManager = storageManager;
        }

        /// <summary>
        /// This is method is used by the "GetPipelineInputFromStorage" activity function to read pipeline inputs from storage
        /// </summary>
        /// <param name="storagePath"></param>
        /// <returns></returns>
        public async Task<string> GetPipelineInputFromStorage(string storagePath)
        {
            logger.LogInformation($"Reading file: {storagePath} from storage");

            var fileContent = await storageManager.ReadTextFileAsync(storagePath);
            return fileContent;
        }

        /// <summary>
        /// This is method is used by the "WriteTextFilesToStorageAsync" activity function to write pipeline outputs to storage
        /// </summary>
        /// <param name="writeToStorageDict"></param>
        /// <returns></returns>
        public async Task<Dictionary<string, string>> WriteTextFilesToStorageAsync(Dictionary<string, string> writeToStorageDict)
        {
            logger.LogInformation($"Writing {writeToStorageDict.Count} file(s) to storage");

            Dictionary<string, string> filePaths = new Dictionary<string, string>();
            foreach (var kv in writeToStorageDict)
            {
                string fileAbsolutePath = await storageManager.WriteTextFileAsync(kv.Value, kv.Key, true);
                filePaths[kv.Key] = fileAbsolutePath;
            }

            return filePaths;
        }

        public List<PipelineStep> UpdateDatasetInPipelineSteps(List<PipelineStep> pipelineSteps, Dictionary<string, string> dataset)
        {
            foreach (var pipelineStep in pipelineSteps)
            {
                if (pipelineStep.StepConfig != null)
                {
                    JToken configToken = pipelineStep.StepConfig["StorageConfiguration"];
                    if (configToken != null && configToken.HasValues)
                    {
                        BlobStorageConfiguration tempStorageConfig = configToken.ToObject<BlobStorageConfiguration>();
                        var storageConfiguration = new BlobStorageConfiguration
                        {
                            OverWriteFile = tempStorageConfig.OverWriteFile,
                            FileFormat = tempStorageConfig.FileFormat,
                            FolderPath = tempStorageConfig.FolderPath,
                            FileNames = new List<string>()
                        };


                        foreach (var fileName in tempStorageConfig.FileNames)
                        {
                            var keyName = fileName.Replace("Dataset.", "");
                            storageConfiguration.FileNames.Add(dataset[keyName]);
                        }

                        //handle folderPath
                        if (!string.IsNullOrEmpty(tempStorageConfig.FolderPath))
                        {
                            var keyName = tempStorageConfig.FolderPath.Replace("Dataset.", "");
                            storageConfiguration.FolderPath = dataset[keyName];
                        }

                        pipelineStep.StepConfig["StorageConfiguration"] = JToken.FromObject(storageConfiguration);
                    }
                }
            }

            return pipelineSteps;
        }

        public string GetPipelineInputFromOutputDictionary(string pipelineStepInput, Dictionary<string, string> outputDictionary)
        {
            var isFound = outputDictionary.TryGetValue(pipelineStepInput, out var value);
            if (isFound)
            {
                return value;
            }
            else
            {
                var message = $"No matching output found for input key: {pipelineStepInput}. Ensure this step is executed before it is used.";
                logger.LogError(message);
                
                throw new Exception(message);
            }
        }

        public List<(string, string)> ParsePipelineStepOutput(string outputResult, PipelineStep pipelineStep)
        {
            var outputs = new List<(string, string)>();

            if (pipelineStep.Outputs?.Count == 0)
            {
                outputs.Add((pipelineStep.StepID, outputResult));
            }
            else
            {
                var validOutputKeys = pipelineStep.Outputs?.Where(k => !k.StartsWith("Dataset.")).ToArray();
                if (validOutputKeys.Length == 1)
                {
                    var key = $"{pipelineStep.StepID}.{validOutputKeys[0]}";
                    outputs.Add((key, outputResult));
                }
                else
                {
                    var stepOutputs = JArray.Parse(outputResult);

                    for (var i = 0; i < stepOutputs.Count; i++)
                    {
                        var key = $"{pipelineStep.StepID}.{validOutputKeys[i]}";
                        // assuming individual elements in the output array are serialized
                        outputs.Add((key, stepOutputs[i].ToString()));
                    }
                }
            }

            return outputs;
        }

        public List<string> GetValidPipelineStepOutputs(PipelineStep pipelineStep, HashSet<string> encounteredOutputStepIDs)
        {
            if (pipelineStep.StepID == null)
            {
                throw new ArgumentException("Pipeline steps must contain StepID for referenceing. Please add a unique StepID to step.");
            }

            if (encounteredOutputStepIDs.Contains(pipelineStep.StepID))
            {
                throw new ArgumentException("StepIDs must be unique, StepID: " + pipelineStep.StepID + " found twice in the pipeline definition,");
            }

            var stepOutputs = new List<string>();

            if (pipelineStep.Inputs != null && pipelineStep.Inputs.Any())
            {
                foreach (var dependency in pipelineStep.Inputs)
                {
                    if ( !dependency.StartsWith("Dataset.") && 
                        !encounteredOutputStepIDs.Contains(dependency))
                    {
                        throw new ArgumentException("Step " + dependency + " defined as input to step " + pipelineStep.StepID + " before it has been run");
                    }
                }
            }

            if (pipelineStep.Outputs != null && pipelineStep.Outputs.Any())
            {
                foreach (var output in pipelineStep.Outputs)
                {
                    if (!output.StartsWith("Dataset."))
                    {
                        stepOutputs.Add(pipelineStep.StepID + "." + output);
                    }
                    else
                    {
                        stepOutputs.Add(output);
                    }
                }
            }
            else
            {
                stepOutputs.Add(pipelineStep.StepID);
            }

            return stepOutputs;
        }

        public void ValidatePipeline(List<PipelineStep> pipelineSteps)
        {
            var encounteredOutputStepIDs = new HashSet<string>();
            foreach (var pipelineStep in pipelineSteps)
            {
                var executionMode = pipelineStep.ExecutionMode;
                if (executionMode == StepExecutionModeEnum.Parallel)
                {
                    var parallelStepOutputs = new List<string>();
                    foreach (var parallelStep in pipelineStep.PipelineSteps)
                    {
                        List<string> stepOutputs = GetValidPipelineStepOutputs(parallelStep, encounteredOutputStepIDs);
                        parallelStepOutputs.AddRange(stepOutputs);
                    }

                    encounteredOutputStepIDs.UnionWith(parallelStepOutputs);
                }
                else
                {
                    List<string> stepOutputs = GetValidPipelineStepOutputs(pipelineStep, encounteredOutputStepIDs);
                    encounteredOutputStepIDs.UnionWith(stepOutputs);
                }
            }
        }

    }
}
