using System.Collections.Generic;

namespace FoxTunes
{
    public static class OpenAIRuntimeConfiguration
    {
        public const string SECTION = "701433D1-5B14-4AAB-A58D-F895A7D5F136";

        public const string API_KEY = "AAAAE6C8-078F-49D3-B7B4-1E39B86DD32E";

        public const string DEFAULT_API_KEY = "";

        public const string MODEL = "BBBB55F5-5B58-4D09-8588-6A3E1DB273EE";

        public const string DEFAULT_MODEL = "gpt-4o-mini";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.OpenAIRuntimeConfiguration_Section)
                .WithElement(new TextConfigurationElement(API_KEY, Strings.OpenAIRuntimeConfiguration_ApiKey)
                    .WithValue(DEFAULT_API_KEY)
                    .WithFlags(ConfigurationElementFlags.Secret))
                .WithElement(new TextConfigurationElement(MODEL)
                    .WithValue(DEFAULT_MODEL)
            );
        }
    }
}
