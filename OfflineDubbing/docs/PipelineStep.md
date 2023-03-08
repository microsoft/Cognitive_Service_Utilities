# Overview
This document describes the different properties of a pipeline step.

## StepOrder, required
- Purpose: Defines the order in which the pipeline steps will be executed by the framework.
- Data type: Int.
- Expectation: Unique values across Pipeline. Lower values means they will be executed before a step with higher value.

## StepName, required
- Purpose: To match module name to execute as part of the pipeline. This should match one of the values in the StepNameEnum class.
- Data type: Enum.
- Exepectation: The evaluation framework solution implements a couple of Azure AI platform services as modules. Corresponding to these modules, Activity Trigger functions are implemented in the code. When pipeline runs, the StepName property is matched to the Activity Trigger function by name and then the resulting module is executed.
### Note:
1. StepName **ParallelStep** should be used for pipeline steps that have child pipeline steps that needs to be run in parallel.

## StepID, required
- Purpose: Unique value (string) that uniquely identifies a pipeline step inside a pipeline. This value is also used to prepare unique input and output names for the Inputs and Outputs properties of the pipeline step.

## StepConfig, required
- Purpose: Each pipeline step can provide its own configuration using StepConfig property.
- Data type: Dictionary.

### Example: 
```
"StepConfig": {
                "IsInputSegmented": false,
                "Route": "/translate?api-version=3.0&from=en&to=es",
                "Endpoint": "https://api.cognitive.microsofttranslator.com"
            },
```
Here, the "Route" property in the StepConfig is used to provide api-version, source and destination language to be used when calling the translation services Rest API in the translator step.

## ExecutionMode, optional
- Purpose: To identify how the pipeline step should be executed. It should match one of the two values (Series or Parallel) of the **StepExecutionModeEnum** class.
- Data type: Enum.
- Expectation: A pipeline step can have child pipeline steps. Child pipeline steps are used if those child modules need to be executed in parallel. The default value for ExecutionMode is **Series**. Samples of setting up a pipeline with parallel steps can be seen in the [Sample Json for different types of pipelines](./SamplePipelineJson.md) document.


## Outputs, optional
- Data type: List of strings representing all the outputs. Output names need to be seperated my comma ",".
- Purpose: To specify how to store the output(s) of a pipeline step. Pipeline step outputs can be stored in three different ways: 
> + In-memory with no custom step name. If a pipeline step does not have have any outputs defined, then the output is saved in an in-memory dictionary with the StepID as the key.
> + In-memory with a custom step name. The outputs from the step are saved in the same order they are produced by the step to an in-memory dictionary with "StepID.OutputName" as the key, where OutputName is the corresponding output name specified in the array.
> + To blob-store. If an element of the Outputs list is prefixed with the string "Dataset." then the output for the previous element is written to the blob store at the file location specified by the dataset name.
### Note:
1. To store an output in blob store, the list element must contain the string **Dataset.** prefixing the dataset name. This will match that output with the dataset name specified for the pipeline.
2. Matching of outputs to write to blob store is done sequentially. First one is the output from the pipeline step. If the second output name starts with **Dataset.**, then the first output is also written to the blob store pointed by the dataset name.
3. Only text files are supported as outputs for blob store.
### Example:
```
{
    "Input": "",
    "Dataset": {
        "STT_WavFile": "src/audio/SourceAudioFileForTranscription.wav",
        "STT_RecognitionResult": "output/stt/recognitionResult.json",
    },
    "Pipeline": {
        "Name": "Transcription",
        "PipelineSteps": [
            {
                "StepOrder": 1,
                "StepName": "SpeechToText",
                "StepID": "SpeechToText",
                "StepConfig": {
                    "EndpointId": "",
                    "IsDetailedOutputFormat": false,
                    **Removed for brevity**
                },
                "Outputs":["RecognitionResult","Dataset.STT_RecognitionResult" ],
            },
```
In the above example, the Speech-to-Text module produces a recognition result (either as text or a detailed result as a complex json object). The output produced is stored an in-memory dictionary with key **SpeechToText.RecognitionResult**. The same is also written to the blob store. When writing to blob store, the dataset name is matched to **STT_RecognitionResult** key in the Dataset section of the pipeline to get the destination blob. In this example the output produced from the Speech To Text module is also written at the location "output/stt/recognitionResult.json" in blob store.

## Inputs
- Purpose: To provide inputs for the pipeline step to process. Inputs can come from three places:
> + Output of the previous step. This is by default. If no inputs are specified, the outputs from the previous step are passed as the inputs to the current step.
> + Specific outputs from any of the previous steps. This is done by specifying the input as "StepID.OutputName".
> + From blob store. This is done by specifying the dataset name with the prefix **Dataset.**. The string **Dataset.** is used to identify if the requested input is coming from the pipeline dataset.
- Data type: List of strings representing all the inputs. Input names need to be seperated my comma ",".
### Note:
1. Creating a StepID with **Dataset.** in its value will cause confusion with Dataset section of the pipeline.
2. Only text files are supported as inputs from blob store.
### Examples:
```
{
    "Inputs": ["SpeechToText.RecognitionResult"],
    "StepName": "Translation",
    "StepOrder": 4,
    "StepID": "Translation2",
}
```
Here the translator step is requesting the input by name. The input is the output named "RecognitionResult" produced by the step with ID "SpeechToText" and was stored in the in-memory dictionary with key "SpeechToText.RecognitionResult".

```
{
    "Inputs": ["Dataset.STT_RecognitionResult"],
    "StepName": "Translation",
    "StepOrder": 3,
    "StepID": "Translation1"
}
```
Here the translator step is requesting the input from blob store. The input is stored in the blob store pointed by the dataset name "STT_RecognitionResult".

## PipelineSteps
- Purpose: To provide a set of child pipeline steps that can be executed in parallel.
- Datatype: List of pipeline steps.
- Expectation: Use PipelineSteps to configure additional pipeline steps that need to be executed in parallel. Use PipelineSteps when ExecutionMode is set to **Parallel**. The parent pipeline step just acts as a container for children steps which could be executed in parallel. To ensure proper inputs and outputs are passed from predecessor and to the successor pipeline steps, it is suggested that pipeline steps use named Inputs and Outputs. Samples of setting up a pipeline with parallel steps can be seen in the [Sample Json for different types of pipelines](./SamplePipelineJson.md) document.

## Support for reading and writing binary files from and to blob store
The framework also has helper methods that the pipeline step modules can use to read and write binary files. Additional configuration for these files can be provided using the **StorageConfiguration** section of the **StepConfig** property. To use these helper methods, the module implementation class should inherit from the **ExecutePipelineStep** abstract class.
### Example: 
Speech-to-Text module needs to read wav files to do transcription. If the wav files are in the same blob storage container, then they can specified in the pipeline step config like below:
```
{
    "StepOrder": 1,
    "StepName": "SpeechToText",
    "StepID": "SpeechToText",
    "StepConfig": {
        "EndpointId": "",
        "IsDetailedOutputFormat": false,
        "StorageConfiguration": {
            "FileNames": ["Dataset.STT_WavFile"],
            "FileFormat": "Binary"
        }
    },
    "Outputs":["RecognitionResult","Dataset.STT_RecognitionResult" ]
}

```
When the pipeline runs, any references to dataset names in the StorageConfiguraiton section is replaced with the dataset value. In the above example, **Dataset.STT_WavFile** is replaced with **src/audio/SourceAudioFileForTranscription.wav**. Inside the Speech-to-Text module, the module writer can then call the **ReadBinaryFilesAsync** method like below to get the binary data needed to pass to the Azure Speech SDK to do the transcription. The ReadBinaryFilesAsync method expects a list of file paths (relative paths) and returns a corresponding list of byte[] array. 
```
List<byte[]> binaryFiles = await ReadBinaryFilesAsync(filePaths);
```
