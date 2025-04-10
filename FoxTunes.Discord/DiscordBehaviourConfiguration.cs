using System.Collections.Generic;

namespace FoxTunes
{
    public static class DiscordBehaviourConfiguration
    {
        public const string SECTION = "3202EF4C-7643-417C-A07C-926FDCE279EF";

        public const string ENABLED = "AAAA0FEC-C50C-4296-BE5C-7AC8D94EF9A6";

        public const string STATE_SCRIPT = "BBBB2666-6F4A-45BA-825E-FFE82E73CF87";

        public const string DETAILS_SCRIPT = "CCCC0548-618C-4014-95E3-2D6C623B44A7";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.DiscordBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.DiscordBehaviourConfiguration_Enabled))
                .WithElement(new TextConfigurationElement(STATE_SCRIPT, Strings.DiscordBehaviourConfiguration_StateScript, path: Strings.General_Advanced)
                    .WithValue(Resources.State)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(DETAILS_SCRIPT, Strings.DiscordBehaviourConfiguration_DetailsScript, path: Strings.General_Advanced)
                    .WithValue(Resources.Details)
                    .DependsOn(SECTION, ENABLED)
            );
        }
    }
}
