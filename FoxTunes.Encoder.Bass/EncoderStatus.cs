using System;

namespace FoxTunes
{
    [Serializable]
    public class EncoderStatus
    {
        public EncoderStatus(EncoderStatusType type)
        {
            this.Type = type;
        }

        public EncoderStatusType Type { get; private set; }
    }

    public enum EncoderStatusType
    {
        None = 0,
        Complete = 1,
        Error = 2
    }
}
