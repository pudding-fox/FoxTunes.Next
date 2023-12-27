using System;

namespace FoxTunes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class ComponentAttribute : Attribute
    {
        public ComponentAttribute(string id, string slot = ComponentSlots.None, string name = null, string description = null)
        {
            this.Id = id;
            this.Slot = slot;
            this.Name = name;
            this.Description = description;
        }

        public string Id { get; private set; }

        public string Slot { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }
    }
}
