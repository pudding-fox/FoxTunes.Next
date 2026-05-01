using System.Collections.Generic;

namespace FoxTunes
{
    public static class RadioBrowserBehaviourConfiguration
    {
        public const string SECTION = "1D61F93D-EAA1-4D24-8418-BDB3FCE433C4";

        public const string ENABLED = "AAAA4FF2-843E-4606-92B8-985611B2CC0A";

        public const string BASE_URL = "BBBB6FD9-FA62-4035-B808-FDAF5BA487D0";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.RadioBrowserBehaviourConfiguration_Section)
                    .WithElement(new BooleanConfigurationElement(ENABLED, Strings.RadioBrowserBehaviourConfiguration_Enabled).WithValue(false))
                    .WithElement(new TextConfigurationElement(BASE_URL, Strings.RadioBrowserBehaviourConfiguration_BaseUrl).WithValue("all.api.radio-browser.info").DependsOn(SECTION, ENABLED)
            );
        }
    }
}
