using System.Collections.Generic;

namespace FoxTunes
{
    public static class AIArtistConfiguration
    {
        public const string SECTION = "0B45C373-4F27-4731-9A90-4C68CDCB358D";

        public const string PROMPT_TEMPLATE = "AAAA95C8-C824-4519-9F8E-B7132351FB50";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.AIArtistConfiguration_Section)
                .WithElement(new TextConfigurationElement(PROMPT_TEMPLATE, Strings.AIArtistConfiguration_PromptTemplate)
                    .WithValue(Strings.AIArtistConfiguration_DefaultPromptTemplate)
                    .WithFlags(ConfigurationElementFlags.MultiLine));
        }
    }
}
