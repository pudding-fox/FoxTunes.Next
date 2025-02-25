using System;

namespace FoxTunes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ComponentPreferenceAttribute : Attribute
    {
        public const byte DEFAULT = 0;

        public const byte NORMAL = 100;

        public const byte LOW = 255;

        public ComponentPreferenceAttribute()
        {
            this.Priority = LOW;
            this.ReleaseType = ReleaseType.None;
        }

        public ComponentPreferenceAttribute(byte priority)
        {
            this.Priority = priority;
            this.ReleaseType = ReleaseType.None;
        }

        public ComponentPreferenceAttribute(ReleaseType releaseType)
        {
            this.Priority = NORMAL;
            this.ReleaseType = releaseType;
        }

        public ComponentPreferenceAttribute(byte priority, ReleaseType releaseType)
        {
            this.Priority = priority;
            this.ReleaseType = releaseType;
        }

        public byte Priority { get; private set; }

        public ReleaseType ReleaseType { get; private set; }

        public bool IsDefault
        {
            get
            {
                if (this.Priority == DEFAULT)
                {
                    return true;
                }
                if (this.ReleaseType == Publication.ReleaseType)
                {
                    return true;
                }
                return false;
            }
        }
    }
}
