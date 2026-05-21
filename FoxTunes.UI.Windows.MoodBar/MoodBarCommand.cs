using System;

namespace FoxTunes
{
    [Serializable]
    public class MoodBarCommand
    {
        public MoodBarCommand(MoodBarCommandType type)
        {
            this.Type = type;
        }

        public MoodBarCommandType Type { get; private set; }
    }

    public enum MoodBarCommandType
    {
        None = 0,
        Cancel = 1,
        Quit
    }
}
