using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.STT
{
    public interface ISpeechToText
    {
        Task<string> DoTranscription(SpeechInput input);
    }
}
