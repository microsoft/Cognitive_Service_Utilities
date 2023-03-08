# Overview
This document provides details about the step configurations in the PreProcess Text-To-Speech (PreprocessTTSStep) module. All the configurations defined below go into the "StepConfig" section of the PreprocessTTSStep pipeline step.

## Configurations:
### TranslationMappingMethod
- Purpose: Defines the method used to calculate speech rate and placement of translated segments in the output audio file.
- Methods:
    - StandardScaleAndFit: The translated segments are placed in exactly the same location as in the input, if the translated segment is longer than source, the rate of speech is sped up to fit in the given span.
    - CompensatePauses_AnchorMiddle: The rate of translated segment is scaled according to how fast/slow the speaker in the input was talking relative to the mean speech rate for the source language. The translated segments are then placed such that the center of the input span aligns with the center of the output span. If the translated segment is longer than the input, it will eat into the pauses in speech before and after the segment.
    - CompensatePauses_AnchorStart: The rate of translated segment is scaled according to how fast/slow the speaker in the input was talking relative to the mean speech rate for the source language. The translated segments are then placed such that the start of the source span aligns with the start of the translated span. If the translated segment is longer than the input, it will eat into the pause in speech after the segment.

**Note** For the **CompensatePauses_AnchorMiddle** and **CompensatePauses_AnchorStart** best effort attempt is made to fit the translated text into the input span, however if the translated text if still too long to fit into the given span, the speech rate will be increased.

- Datatype: Enum

### MaxSpeechRate
- Purpose: To set the upper limit of the Speech Rate used for all the segment. This is typically set to prevent the speech from sounding unnaturally fast. 
- Datatype: double

**Note**: The speech rate refers to a multiplier, for example a speech rate of 1.5 refers to 1.5 times the standard speech rate for a given text (https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/speech-synthesis-markup#adjust-prosody)
**Note**: The module will try to use this rate if possible, but if the segment needs to be sped up further to fit in the span available, this value might be overridden.
**Note**: This configuration is only used when TranslationMappingMethod is either set to **CompensatePauses_AnchorMiddle** or **CompensatePauses_AnchorStart**.

### MinSpeechRate
- Purpose: To set the lower limit of the Speech Rate used for all the segment. This is typically set to prevent the speech from sounding unnaturally slow. 
- Datatype: double

**Note**: This configuration is only used when TranslationMappingMethod is either set to **CompensatePauses_AnchorMiddle** or **CompensatePauses_AnchorStart**.

### VoiceMapping
- Purpose: Define the voices to be used for different idetentified speakers in the input
    - Each entry in this dictionary represents a mapping of Identified Speaker and Locale (**SpeakerName**_**Locale**) to the voice that needs to be used for speech synthesis.
    - For each of these entries **VoiceName** defines the voice in the Azure Text-To-Speech service. (For full list of voices: https://learn.microsoft.com/en-us/azure/cognitive-services/speech-service/language-support?tabs=stt-tts).
    - If for any of the speakers and locale combination a custom neural voice is registered, the **EndpointID** property can be used to specify the custom neural voice endpointID.
    - When Speech Diarization service isn't able to identify a speaker, it labels it as "UnIdentified". VoiceMapping dictionary should also include a mapping for Unidentified speaker so the right voice is used for those segments.
    - For other unspecifed input voices you can use **Default**_**Locale** for setting a voice.

- Datatype: Dictionary
- Example: 
    ```  
    {
        "VoiceMapping": {
                    "Unidentified_en-US": {
                        "VoiceName": "en-US-GuyNeural",
                        "EndpointID": ""
                    },
                    "Stuart_en-US": {
                        "VoiceName": "",
                        "EndpointID": "<URL-TO-CUSTOM-VOICE-ENDPOINT>"
                    },
                    "Mike_en-US": {
                        "VoiceName": "en-US-BrandonNeural",
                        "EndpointID": ""
                    },
                    "Default_en-US": {
                        "VoiceName": "en-US-JennyNeural",
                        "EndpointID": ""
                    },                   
        }
    }
    ```
**Note**:  In the following example, when the speech recognition locale is en-US and Speech Diarization service had detected "Mike" as the speaker, then in this module, the Voice "en-US-BrandonNeural" from Azure Text-To-Speech service will be used to produce Text-to-Speech (audio file) for segments where Mike is identified as the speaker.

## Example:
```  
{
    "PipelineSteps": [
        {
        "StepName": "PreprocessTTS",
        "StepOrder": 1,
        "StepID": "PreprocessTTSStep",
        "StepConfig": {
          "VoiceMapping": {
            "Unidentified_en-US": {
              "VoiceName": "en-US-GuyNeural",
              "EndpointID": ""
            },
            "Guest_0_en-US": {
              "VoiceName": "en-US-JennyNeural",
              "EndpointID": ""
            },
            "Guest_1_en-US": {
              "VoiceName": "en-US-BrandonNeural",
              "EndpointID": ""
            },
            "Guest_2_en-US": {
              "VoiceName": "en-US-ElizabethNeural",
              "EndpointID": ""
            },
            "Default_en-US": {
              "VoiceName": "en-US-JennyMultilingualNeural",
              "EndpointID": ""
            }
          },
          "TranslationMappingMethod": "CompensatePauses_AnchorStart",
          "MaxSpeechRate": "1.250",
          "MinSpeechRate": "0.750"
        }
    ]
}
```
Note: For details about properties: StepOrder, StepName, StepId, StorageConfiguration go to PipelineStep.md for reference. (./pipeline.md)