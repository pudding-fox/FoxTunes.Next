using System.Collections.Generic;

namespace FoxTunes
{
    public static class LayoutDesignerKeyBindingsBehaviourConfiguration
    {
        public const string SECTION = InputManagerConfiguration.SECTION;

        public const string EDIT_ELEMENT = "JJJJ5E20-668D-40A4-BF9D-514A8A743C61";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(
                    new TextConfigurationElement(EDIT_ELEMENT, Strings.LayoutDesignerKeyBindingsBehaviourConfiguration_Edit)
                        .WithValue("Alt+L")
            );
        }
    }
}
