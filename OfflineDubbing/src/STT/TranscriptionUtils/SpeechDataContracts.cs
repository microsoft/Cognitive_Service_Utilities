using System.Runtime.Serialization;

namespace AIPlatform.TestingFramework.STT.TranscriptionUtils
{
    [DataContract]
    internal class VoiceSignature
    {
        [DataMember]
        public string Status { get; private set; }

        [DataMember]
        public VoiceSignatureData Signature { get; private set; }

        [DataMember]
        public string Transcription { get; private set; }
    }

    [DataContract]
    internal class VoiceSignatureData
    {
        internal VoiceSignatureData()
        { }

        internal VoiceSignatureData(int version, string tag, string data)
        {
            Version = version;
            Tag = tag;
            Data = data;
        }

        [DataMember]
        public int Version { get; private set; }

        [DataMember]
        public string Tag { get; private set; }

        [DataMember]
        public string Data { get; private set; }
    }

}
