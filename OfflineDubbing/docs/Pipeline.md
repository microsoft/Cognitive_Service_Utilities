# Overview
This document provides overview of the different elements of a pipeline and how to use that to compose one.

## Parts of Pipeline
Here are top sections of a pipeline.

### Input
- Purpose: To provide input to the pipeline. 
- Data Type: Text.

### ExpectedOutput
- Purpose: Used for evaluation purposes. For the given input what is the expected output after the complete pipeline is run. This will be used to compare with the actual output produced from the pipeline.
- Data Type: Text/Json.
- Current Status: NOT IMPLEMENTED.

### Dataset
- Purpose: Use to specify paths (relative to the container specified in the settings.json file) to the files in the blob store. This could be used as Input to a Pipeline step, or used as an output destination to store results from a pipeline step.
- DataType: Dictionary of strings representing the dataset name mapping to a string representing a file path in the blob store.

Example: 
```
"Dataset": {
        "STT_WavFile": "src/audio/SourceAudioFileForTranscription.wav",
        "STT_RecognitionResult": "output/stt/recognitionResult.json",
        "TTS_Audio": "output/tts/DubbedAudioFile.wav"
    },
```
Here **STT_WavFile** is the dataset name pointing to the **src/audio/SourceAudioFileForTranscription.wav** wav file in the blob store. **STT_WavFile** can be later used as Input in the PipelineStep's Input property.

### Pipeline
- Purpose: This is a container for all the PipelineSteps. It contain two properties:
> + Name: This is string value to provide a name to the Pipeline. We could use this property to query for application logs in Application Insights.
> + PipelineSteps: This is an array of pipeline steps that make this pipeline. Each pipeline step has certain properties specific to it. More details of the Pipeline Step can be found in the [PipelineStep.md](./PipelineStep.md) document. 

### Sample Pipeline Json
```
{
    "Input": "hello how are you",
    "Dataset": {
        "STT_WavFile": "src/audio/SourceAudioFileForTranscription.wav",
        "STT_RecognitionResult": "output/stt/recognitionResult.json",
        "TTS_Audio": "output/tts/DubbedAudioFile.wav"
    },
    "Pipeline": {
        "Name": "Dubbing",
        "PipelineSteps": []
    }
}
```

## Other Links
- [Pipeline Step.](./PipelineStep.md)
- [Sample Json for different types of pipelines.](./SamplePipelineJson.md)
