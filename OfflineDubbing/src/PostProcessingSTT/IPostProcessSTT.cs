namespace AIPlatform.TestingFramework.PostProcessingSTT
{
    public interface IPostProcessSTT
    {
        public string DoSpeechToTextPostProcessing(PostProcessSTTInput input);
    }
}
