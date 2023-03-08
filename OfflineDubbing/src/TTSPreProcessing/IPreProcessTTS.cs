using System.Collections.Generic;
using System.Threading.Tasks;

namespace AIPlatform.TestingFramework.TTSPreProcessing
{
    public interface IPreProcessTTS
    {
        string DoTTSPreProcessing(List<PreProcessTTSInput> inputs);
    }
}
