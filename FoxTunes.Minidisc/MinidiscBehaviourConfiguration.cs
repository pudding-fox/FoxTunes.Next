using System.Collections.Generic;

namespace FoxTunes
{
    public static class MinidiscBehaviourConfiguration
    {
        public const string SECTION = "4F81FE99-1897-4C0D-A978-C2A4790D123D";

        public const string ENABLED = "AAAACAEF-31D9-49B6-A0A9-0B3E55050D0C";

        public const string DISC_TITLE_SCRIPT = "BBBBF58E-1129-4926-A54B-B1BD7E522756";

        public const string TRACK_NAME_SCRIPT = "CCCC3251-B5B6-4A66-AE94-68EA093BC93D";

        public const string CLEANUP = "ZZZZ5B81-7D48-444D-93F4-B3CF133E9383";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.MinidiscBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.MinidiscBehaviourConfiguration_Enabled)
                    .WithValue(false))
                .WithElement(new TextConfigurationElement(DISC_TITLE_SCRIPT, Strings.MinidiscBehaviourConfiguration_DiscTitleScript, path: Strings.MinidiscBehaviourConfiguration_Advanced)
                    .WithValue(Resources.DiscTitle)
                    .WithFlags(ConfigurationElementFlags.MultiLine | ConfigurationElementFlags.Script)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(TRACK_NAME_SCRIPT, Strings.MinidiscBehaviourConfiguration_TrackNameScript, path: Strings.MinidiscBehaviourConfiguration_Advanced)
                    .WithValue(Resources.TrackName)
                    .WithFlags(ConfigurationElementFlags.MultiLine | ConfigurationElementFlags.Script)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new CommandConfigurationElement(CLEANUP, Strings.MinidiscBehaviourConfiguration_Cleanup)
                    .WithHandler(() => MinidiscTrackFactory.Cleanup())
                    .DependsOn(SECTION, ENABLED)
            );
        }
    }
}
