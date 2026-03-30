using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing.Printing;

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

        public const string REASONING_LEVEL = "BCCCE875-EB27-41C7-8FF4-0879837030E5";

        public const string REASONING_LEVEL_NONE = "AAAAFE8F-C940-44B1-82AE-CE8840E0D641";

        public const string REASONING_LEVEL_MINIMAL = "BBBB06C0-7819-4267-9C2B-C13CD5C13574";

        public const string REASONING_LEVEL_LOW = "CCCCD036-7FC4-4E8B-B72E-802B8DC3903B";

        public const string REASONING_LEVEL_MEDIUM = "DDDD9439-05EC-439B-BC2E-16BFF0BBAFB5";

        public const string REASONING_LEVEL_HIGH = "EEEE2823-BE5B-464E-B617-E8A88995F8B4";

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
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new SelectionConfigurationElement(REASONING_LEVEL, Strings.OpenAIRuntimeConfiguration_ReasoningLevel)
                    .WithOptions(GetReasoningLevels())
                    .DependsOn(SECTION, ENABLED)
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetReasoningLevels()
        {
            yield return new SelectionConfigurationOption(REASONING_LEVEL_NONE, Strings.OpenAIRuntimeConfiguration_ReasoningLevel_None);
            yield return new SelectionConfigurationOption(REASONING_LEVEL_MINIMAL, Strings.OpenAIRuntimeConfiguration_ReasoningLevel_Minimal);
            yield return new SelectionConfigurationOption(REASONING_LEVEL_LOW, Strings.OpenAIRuntimeConfiguration_ReasoningLevel_Low);
            yield return new SelectionConfigurationOption(REASONING_LEVEL_MEDIUM, Strings.OpenAIRuntimeConfiguration_ReasoningLevel_Medium).Default();
            yield return new SelectionConfigurationOption(REASONING_LEVEL_HIGH, Strings.OpenAIRuntimeConfiguration_ReasoningLevel_High);
        }

        public static ReasoningLevel GetReasoningLevel(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                case REASONING_LEVEL_NONE:
                    return ReasoningLevel.None;
                case REASONING_LEVEL_MINIMAL:
                    return ReasoningLevel.Minimal;
                case REASONING_LEVEL_LOW:
                    return ReasoningLevel.Low;
                default:
                case REASONING_LEVEL_MEDIUM:
                    return ReasoningLevel.Minimal;
                case REASONING_LEVEL_HIGH:
                    return ReasoningLevel.High;
            }
        }
    }
}
