using System.Collections.Generic;
using System.Collections.Specialized;

namespace FoxTunes
{
    public static class OpenAIRuntimeConfiguration
    {
        public const string SECTION = AIBehaviourConfiguration.SECTION;

        public const string ENABLED = AIBehaviourConfiguration.ENABLED;

        public const string API_KEY = "AAAAE6C8-078F-49D3-B7B4-1E39B86DD32E";

        public const string DEFAULT_API_KEY = "";

        public const string MODEL = "BBBB55F5-5B58-4D09-8588-6A3E1DB273EE";

        public const string DEFAULT_MODEL = "gpt-5.4-nano";

        public const string TEMPERATURE = "BBCC6CEF-3634-484A-93F4-46F2D28510C2";

        public const double TEMPERATURE_MIN = 0d;

        public const double TEMPERATURE_MAX = 2d;

        public const double TEMPERATURE_DEFAULT = 1d;

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new TextConfigurationElement(API_KEY, Strings.OpenAIRuntimeConfiguration_ApiKey)
                    .WithValue(DEFAULT_API_KEY)
                    .WithFlags(ConfigurationElementFlags.Secret)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(MODEL, Strings.OpenAIRuntimeConfiguration_Model)
                    .WithValue(DEFAULT_MODEL)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new DoubleConfigurationElement(TEMPERATURE, Strings.OpenAIRuntimeConfiguration_Temperature)
                    .WithValue(TEMPERATURE_DEFAULT)
                    .WithValidationRule(new DoubleValidationRule(TEMPERATURE_MIN, TEMPERATURE_MAX, 0.1d))
                    .DependsOn(SECTION, ENABLED)
            );
        }
    }
}
