using System.Collections.Generic;

namespace FoxTunes
{
    public static class AITrackConfiguration
    {
        public const string SECTION = "77004AB8-DC42-470D-B435-6A4221BEE75B";

        public const string PROMPT_TEMPLATE = "2377F463-16FB-4180-B350-DC1975293223";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.AITrackConfiguration_Section)
                .WithElement(new TextConfigurationElement(PROMPT_TEMPLATE, Strings.AITrackConfiguration_PromptTemplate)
                    .WithValue(Strings.AITrackConfiguration_DefaultPromptTemplate)
                    .WithFlags(ConfigurationElementFlags.MultiLine));
        }
    }
}
