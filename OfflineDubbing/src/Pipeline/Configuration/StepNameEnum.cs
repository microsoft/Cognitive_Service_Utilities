using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AIPlatform.TestingFramework.Pipeline.Configuration
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum StepNameEnum
    {
        None,           //so by default StepName is not SpeechToText
        SpeechToText,
        PostProcessSTT,
        SpeechCorrectnessEvaluation,
        TextToSpeech,
        PreProcessTTS,
        Translation,
        TranslationCorrectnessEvaluation,
        SentimentAnalysis,
        LanguageUnderstanding,
        ConversationalLanguageUnderstanding,
        Classification,
        ContentModerator,
        ImageRead,
        CustomCode,
        AzureCognitiveSearch,
        AzureOpenAI,
        AOAI_GetAgentResponse,
        SubtitlesWriting,
        BatchTest,
        EvaluateModel,
        ParallelStep,
    }
}
