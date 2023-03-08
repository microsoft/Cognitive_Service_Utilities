using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.TTS
{
    public interface ITextToSpeech
    {
        Task<string> GenerateAudioAsync (TextToSpeechInput input);
    }
}
