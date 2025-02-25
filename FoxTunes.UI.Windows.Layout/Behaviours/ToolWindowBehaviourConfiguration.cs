using System.Collections.Generic;

namespace FoxTunes
{
    public static class ToolWindowBehaviourConfiguration
    {
        public const string SECTION = "1996EE69-990F-4E69-9785-1BF68D5CC9F7";

        public const string ELEMENT = "38DCA76A-13BD-49AB-A3D9-2598A47ED01D";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new TextConfigurationElement(ELEMENT).WithValue(string.Empty))
                .WithFlags(ConfigurationSectionFlags.System);
        }
    }
}
