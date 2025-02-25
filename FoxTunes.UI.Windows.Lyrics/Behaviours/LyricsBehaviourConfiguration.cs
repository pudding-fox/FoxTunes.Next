using System.Collections.Generic;

namespace FoxTunes
{
    public static class LyricsBehaviourConfiguration
    {
        public const string SECTION = "42FB4DBD-E28C-4E42-B64F-6921CFCEF924";

        public const string AUTO_SCROLL = "AAAA70DD-84E9-48D5-B174-E0A0FC498698";

        public const string AUTO_LOOKUP = "BBBB3698-E26A-4D6C-9BBF-E845B0F9D150";

        public const string AUTO_LOOKUP_PROVIDER = "BBCC07C6-5584-4E7E-9526-EB60F9F72E49";

        public const string WRITE_TAGS = "EEEE4CCA-250F-4892-9B00-CB0C22D094D3";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.LyricsBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(AUTO_SCROLL, Strings.LyricsBehaviourConfiguration_AutoScroll)
                    .WithValue(true))
                .WithElement(new BooleanConfigurationElement(AUTO_LOOKUP, Strings.LyricsBehaviourConfiguration_AutoLookup)
                    .WithValue(false))
                .WithElement(new SelectionConfigurationElement(AUTO_LOOKUP_PROVIDER, Strings.LyricsBehaviourConfiguration_AutoLookupProvider)
                    .DependsOn(SECTION, AUTO_LOOKUP))
                .WithElement(new BooleanConfigurationElement(WRITE_TAGS, Strings.LyricsBehaviourConfiguration_WriteTags)
                    .WithValue(true)
            );
        }
    }
}
