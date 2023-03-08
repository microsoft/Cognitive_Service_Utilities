# Overview
This document includes examples of sample pipeline jsons.

## Example 1: Parallel Tasks, reading and writing from blob store
- Purpose: Run transcription of a wav file and then do translation in multiple languages in parallel. Store the output from Speech-to-Text to a blob store, so it can be used later on.
### Note
- The Translation1 step reads the input from the blob store while Translation2 module reads it from the in-memory dictionary.

```
{
    "Input": "",
    "Dataset": {
        "STT_WavFile": "src/audio/SourceAudioFileForTranscription.wav",
        "STT_RecognitionResult": "output/stt/recognitionResult.json",
    },
    "Pipeline": {
        "Name": "Dubbing",
        "PipelineSteps": [
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
                "Outputs":["RecognitionResult","Dataset.STT_RecognitionResult" ],
            },
            {
                "StepOrder": 2,
                "StepName": "ParallelStep",
                "StepID":"ParallelSteps",
                "ExecutionMode": "Parallel",
                "PipelineSteps": [
                    {
                        "Inputs": ["Dataset.STT_RecognitionResult"],
                        "StepName": "Translation",
                        "StepOrder": 3,
                        "StepID": "Translation1",
                        "StepConfig": {
                            "IsInputSegmented": false,
                            "Route": "/translate?api-version=3.0&from=en&to=es",
                            "Endpoint": "https://api.cognitive.microsofttranslator.com"
                        },
                        "Outputs":["TranslationResult"]
                    },
                    {
                       "Inputs": ["SpeechToText.RecognitionResult"],
                        "StepName": "Translation",
                        "StepOrder": 4,
                        "StepID": "Translation2",
                        "StepConfig": {
                            "IsInputSegmented": false,
                            "Route": "/translate?api-version=3.0&from=en&to=hi",
                            "Endpoint": "https://api.cognitive.microsofttranslator.com"
                        },
                        "Outputs":["TranslationResult"]
                    }
                ]
            }
        ]
    }
}
```

## Example 2: Diarization Example. It is a short clip from presidential debate where there are 3 speakers taking turns to speak with occasional interruptions
## All outputs are stored in the blob storage for further review
```
{
   "Input": "",
   "Dataset": {
        "STT_WavFile": "src/audio/Presidential Debate 2020 Clip_audio_short.wav",
        "STT_RecognitionResult": "output/short_debate/stt/recognitionResult.json",
        "STT_HumanIntervention": "output/short_debate/stt/humanIntervention.json",
        "STT_SubtitlesResults": "output/short_debate/stt/subtitle.webvtt",
        "STT_TranslationOutput": "output/short_debate/stt/STT_TranslationOutput.json",
        "STT_TTSPreprocessingOutput": "output/short_debate/stt/STT_TTSPreprocessingOutput.json",
        "TranslationOutput": "output/short_debate/translation/translationOutput.json",
        "TranslationCorrectness": "output/short_debate/translation/translationCorrectness.json",
        "TTS_SSML": "output/short_debate/tts/ssml.xml",
        "TTS_WAV_FLDR":"output/short_debate/tts"
    },
    "Pipeline": {        
        "Name": "TranslationEval",
        "PipelineSteps": [
            {
                "StepOrder": 1,
                "StepName": "SpeechToText",
                "StepId": "SpeechToText",
                "StepConfig": {
                    "EndpointId": "",
                    "IsDetailedOutputFormat": true,
                    "Locale": "en-us",
                    "ServiceProperty": {
                    },
                    "ContinuousLID" : {
                        "Enabled": false,
                        "CandidateLocales": ["en-us","hi-in"]
                    },
                    "ConversationTranscription" : {
                        "Enabled": true,
                        "Speakers" : [
                            {
                                "Name": "Aria",
                                "VoiceSample": "<PUT ACTUAL URL HERE>"
                            }
                        ]
                    },
                    "StorageConfiguration": {
                         "FileNames": ["Dataset.STT_WavFile"],
                         "FileFormat": "Binary"
                     }
                 },
                 "Outputs":["STT_RecognitionResult", "Dataset.STT_RecognitionResult"]
            },
            {
                "Inputs": ["Dataset.STT_RecognitionResult"],
                "StepOrder": 2,
                "StepName": "PostprocessSTT",
                "StepID": "PostprocessSTT",
                "StepConfig": {
                    "SourceLocales": ["en-US"],
                    "TargetLocale": "es-MX",
                    "IgnoreUnexpectedSourceLocales": true
                },
                "Outputs": ["TranslationOutput", "TTSPreprocessingOutput", "Dataset.STT_TranslationOutput", "Dataset.STT_TTSPreprocessingOutput"]
            },
            {
                "Inputs": ["PostprocessSTT.TranslationOutput"],
                "StepOrder": 3,
                "StepName": "Translation",
                "StepID": "Translation",
                "StepConfig": {
                    "Endpoint": "https://api.cognitive.microsofttranslator.com",
                    "Route": "/translate?api-version=3.0&from=en&to=es",
                    "IsInputSegmented": "True"
                },
                "Outputs": ["TranslationOutput", "Dataset.TranslationOutput"]
            },
            {
                "Inputs": ["Dataset.STT_TTSPreprocessingOutput", "Dataset.TranslationOutput"],
                "StepOrder": 4,
                "StepName": "TranslationCorrectnessEvaluation",
                "StepID": "TranslationCorrectnessEvaluation",
                "StepConfig": {
                    "Threshold": 0,
                    "TranslatorConfiguration": {
                        "Endpoint": "https://api.cognitive.microsofttranslator.com",
                        "Route": "/translate?api-version=3.0&from=en&to=es",
                        "IsInputSegmented": "True"
                    }
                },
                "Outputs":["TranslationCorrectness", "Dataset.TranslationCorrectness"]
            }
        ]
    }
}
```