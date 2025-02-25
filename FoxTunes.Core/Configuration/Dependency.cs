using System;

namespace FoxTunes
{
    public abstract class Dependency
    {
        protected Dependency(string sectionId, string elementId, bool negate)
        {
            this.SectionId = sectionId;
            this.ElementId = elementId;
            this.Negate = negate;
        }

        public string SectionId { get; private set; }

        public string ElementId { get; private set; }

        public bool Negate { get; private set; }

        public abstract bool Validate(ConfigurationElement element);

        public abstract void AddHandler(ConfigurationElement element, EventHandler handler);
    }
}
