using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassCdStreamProviderBehaviourConfiguration
    {
        public const string SECTION = "220BF762-28B1-436C-951D-5B0359473A40";

        public const string ENABLED = "AAAA1DA2-B933-42AC-8FE7-64BA7D7EA2B8";

        public const string LOOKUP = "BBBB29AB-ED22-4AB2-AD79-0EFE4EAB39B7";

        public const string LOOKUP_HOST = "CCCC87EE-07E2-4B95-8F9D-039738956A30";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.BassCdStreamProviderBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.BassCdStreamProviderBehaviourConfiguration_Enabled).WithValue(false))
                .WithElement(new BooleanConfigurationElement(LOOKUP, Strings.BassCdStreamProviderBehaviourConfiguration_Lookup)
                    .WithValue(true)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(LOOKUP_HOST, Strings.BassCdStreamProviderBehaviourConfiguration_Host)
                    .WithValue(string.Join(Environment.NewLine, GetLookupHosts()))
                    .WithFlags(ConfigurationElementFlags.MultiLine)
                    .DependsOn(SECTION, ENABLED).DependsOn(SECTION, LOOKUP));
        }

        public static IEnumerable<string> GetLookupHosts()
        {
            return new[]
            {
                Strings.BassCdStreamProviderBehaviourConfiguration_Host1,
                Strings.BassCdStreamProviderBehaviourConfiguration_Host2
            };
        }
    }
}
