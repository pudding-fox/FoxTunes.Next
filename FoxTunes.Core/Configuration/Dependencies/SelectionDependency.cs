using System;

namespace FoxTunes
{
    public class SelectionDependency : Dependency
    {
        public SelectionDependency(string sectionId, string elementId, string optionId, bool negate) : base(sectionId, elementId, negate)
        {
            this.OptionId = optionId;
        }

        public string OptionId { get; private set; }

        public override bool Validate(ConfigurationElement element)
        {
            if (element is SelectionConfigurationElement selectionConfigurationElement)
            {
                var result = string.Equals(selectionConfigurationElement.Value.Id, this.OptionId, StringComparison.OrdinalIgnoreCase);
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
            if (element is SelectionConfigurationElement selectionConfigurationElement)
            {
                selectionConfigurationElement.ValueChanged += handler;
            }
        }
    }
}
