using System;

namespace FoxTunes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ComponentPriorityAttribute : Attribute
    {
        public const byte HIGH = 0;

        public const byte NORMAL = 100;

        public const byte LOW = 255;

        public ComponentPriorityAttribute()
        {
            this.Priority = LOW;
        }

        public ComponentPriorityAttribute(byte priority)
        {
            this.Priority = priority;
        }

        public byte Priority { get; set; }
    }
}
