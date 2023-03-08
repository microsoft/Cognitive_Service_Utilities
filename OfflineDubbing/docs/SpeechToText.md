# Overview
This document provides details about the step configurations in the Speech-To-Text (STT) module. All the configurations defined below go into the "StepConfig" section of your Speech-To-Text pipeline step.


## Configurations:
### EndpointId
- Purpose: To define the Speech-To-Text endpoint called during execution (typically used when a custom model is deployed) (For details, refer to: [Custom Speech overview - Speech service - Azure Cognitive Services | Microsoft Learn](https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/custom-speech-overview))

- Datatype: String (URL)

### IsDetailedOutputFormat
- Purpose: To define the output produced by the Speech-To-Text service, 
	- When set to **true** the detailed output for each segment is produced separately including details like: offset, duration, NBest etc.
	- When set to **false** the simple text output for the entire audio is produced as one single string.

- Datatype: bool

**Note**: When either **ContinuousLID** of **ConversationsTranscription** is enabled IsDetailedOutputFormat is automatically set to true.

### Locale
- Purpose: Set the locale (Language and Region) of the expected text.
- Datatype: string
- Example: "en-US", "es-MX" etc.

**Note**: When **ContinuousLID** is enabled, the expected languages set in that configuration are used and this one is ignored.

### ServiceProperty
- Purpose: Set any additional properties for the speech service.
- Datatype: Dictionary
- Example: 
    ```  
    {
        "ServicePropertyName": "ServicePropertyValue"
    }
    ```

### ContinuousLID
- Purpose: Set configuration for the Azure Speech-To-Text Continuous Languge ID feature
- Configurations:
    - Enabled: Enable or disable use of Continuous Language ID (bool: true/false)
    - CandidateLocales: List of expected locales in the input (List of strings: locales)
- Example: 
    ```  
    {
        "Enabled": true,
        "CandidateLocales": [
                                "en-US",
                                "hi-IN"
                            ]
    }
    ```
### ConversationTranscription
- Purpose: Set configuration for the Azure Speech-To-Text Conversational feature (this feature recognizes different speakers in the input audio and labels them based on registered voices or  newly recognized voices) 
(Details: https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/conversation-transcription)
- Configurations:
    - Enabled: Enable or disable use of Conversational Transcription (bool: true/false)
    - Speakers (Optional): List speakers to be registered for recognition: (List: {Name, VoiceSample})
- Example: 
    ```  
    {
        "Enabled": true,
        "Speakers": [
            {
                "Name": "Aria",
                "VoiceSample": "VOICE-SAMPLE-URL-BLOB-STORE"
            },
            {
                "Name": "Jenny",
                "VoiceSample": "VOICE-SAMPLE-URL-BLOB-STORE"
            }
        ]
    }
    ```

**Note**: **VOICE-SAMPLE-URL-BLOB-STORE** for each voice should be a valid audio file located in accessible blob store locations (Details on supported files types: https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/how-to-use-conversation-transcription?pivots=programming-language-javascript#create-voice-signatures)
**Note**: The defined voices are registered in your subscription everytime the pipeline is run.
**Note**: Speaker Registration is optional, if no speakers are defined, generic Identified Speaker names are produced (Guest_0, Guest_1... etc.)

## Example:
```  
{
    "PipelineSteps": [
        {
            "StepOrder": 1,
            "StepName": "SpeechToText",
            "StepId": "SpeechToText",
            "StepConfig": {
                "EndpointId": "<ENDPOINT-URL>",
                "IsDetailedOutputFormat": true,
                "Locale": "en-us",
                "ServiceProperty": {},
                "ContinuousLID": {
                    "Enabled": true,
                    "CandidateLocales": [
                        "en-us",
                        "hi-in"
                    ]
                },
                "ConversationTranscription": {
                    "Enabled": true,
                    "Speakers": [
                        {
                            "Name": "Aria",
                            "VoiceSample": "VOICE-SAMPLE-URL-BLOB-STORE"
                        },
                        {
                            "Name": "Jenny",
                            "VoiceSample": "VOICE-SAMPLE-URL-BLOB-STORE"
                        }
                    ]
                },
                "StorageConfiguration": {
                    "FileNames": [
                        "Dataset.STT_WavFile"
                    ],
                    "FileFormat": "Binary"
                }
            }
        }
    ]
}
```
Note: For details about properties: StepOrder, StepName, StepId, StorageConfiguration go to PipelineStep.md for reference. (./pipeline.md)