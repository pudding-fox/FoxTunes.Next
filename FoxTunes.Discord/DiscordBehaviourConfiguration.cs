using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class DiscordBehaviourConfiguration
    {
        public const string SECTION = "3202EF4C-7643-417C-A07C-926FDCE279EF";

        public const string ENABLED = "AAAA0FEC-C50C-4296-BE5C-7AC8D94EF9A6";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.DiscordBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.DiscordBehaviourConfiguration_Enabled)
            );
        }
    }
}
