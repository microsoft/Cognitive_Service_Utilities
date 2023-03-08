# Overview
This document provides details about the step configurations in the Text-To-Speech (STT) module. All the configurations defined below go into the "StepConfig" section of your Text-To-Speech pipeline step.

## Inputs:
This module accepts SSML as input and generates one audio file in the specified location.
Details of Azure Text-To-Speech SSML: https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-synthesis-markup

## Configurations:
### StorageWriterConfiguration
- Purpose: To define the output folder path of the audio file that is generated.
- Configuration:
    - OverWriteFile: Overwrites file if already present if set to true.
    - FolderPath: The location in blob storage where file will be written

## Example:
```  
{
    "PipelineSteps": [
        {
            "StepOrder": 6,
            "StepName": "TextToSpeech",
            "StepID": "TextToSpeech",
            "StepConfig": {
                "StorageWriterConfiguration": {
                "OverWriteFile": true,
                "FolderPath": "output/build/tts"
                }
        }
    ]
}
```
Note: For details about properties: StepOrder, StepName, StepId, StorageConfiguration go to PipelineStep.md for reference. (./pipeline.md)