using System.Collections.Generic;

namespace FoxTunes
{
    public class LibraryBehaviourConfiguration
    {
        public const string SECTION = "B5D07BB8-1FAD-4681-8F1F-8EB4C6379B27";

        public const string LIVE_UPDATES = "9566976B-0557-42EF-80B9-D5A69E2E768E";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.LibraryBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(LIVE_UPDATES, Strings.LibraryBehaviourConfiguration_LiveUpdates));
        }
    }
}
