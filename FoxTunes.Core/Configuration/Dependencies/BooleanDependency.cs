using System;

namespace FoxTunes
{
    public class BooleanDependency : Dependency
    {
        public BooleanDependency(string sectionId, string elementId, bool negate) : base(sectionId, elementId, negate)
        {

        }

        public override bool Validate(ConfigurationElement element)
        {
            if (element is BooleanConfigurationElement booleanConfigurationElement)
            {
                var result = booleanConfigurationElement.Value;
                if (this.Negate)
                {
                    result = !result;
                }
                return result;
            }
            return false;
        }

        public override void AddHandler(ConfigurationElement element, EventHandler handler)
        {
            if (element is BooleanConfigurationElement booleanConfigurationElement)
            {
                booleanConfigurationElement.ValueChanged += handler;
            }
        }
    }
}
