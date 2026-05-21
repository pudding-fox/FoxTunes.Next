using System;

namespace FoxTunes
{
    [Serializable]
    public class MoodBarStatus
    {
        public MoodBarStatus(MoodBarStatusType type)
        {
            this.Type = type;
        }

        public MoodBarStatusType Type { get; private set; }
    }

    public enum MoodBarStatusType
    {
        None = 0,
        Complete = 1,
        Error = 2
    }
}
