using System;

namespace FoxTunes
{
    [Serializable]
    public class EncoderCommand
    {
        public EncoderCommand(EncoderCommandType type)
        {
            this.Type = type;
        }

        public EncoderCommandType Type { get; private set; }
    }

    public enum EncoderCommandType
    {
        None = 0,
        Cancel = 1,
        Quit
    }
}
