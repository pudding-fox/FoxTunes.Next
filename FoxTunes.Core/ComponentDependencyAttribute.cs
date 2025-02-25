using System;

namespace FoxTunes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class ComponentDependencyAttribute : Attribute
    {
        public ComponentDependencyAttribute()
        {

        }

        public string Id { get; set; }

        public string Slot { get; set; }
    }
}
