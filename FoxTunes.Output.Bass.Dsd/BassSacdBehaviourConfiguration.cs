using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassSacdBehaviourConfiguration
    {
        public const string SECTION = "5A90BCBE-3F74-462B-9646-3C88EA8DC3C8";

        public const string ENABLED = "AAAA6B82-EE43-4B07-B764-690CE65E88DD";

        public const string AREA = "BBBB51AB-A920-436E-B3F5-9A54CECE7244";

        public const string AREA_STEREO = "AAAA530B-61D3-4BAD-8F49-5E807A6E677A";

        public const string AREA_MULTI_CHANNEL = "BBBBFA1F-75B0-4593-B74F-B70BB09D5333";

        public const string CLEANUP_ELEMENT = "ZZZZ2AB1-B938-4B57-9989-AADADC4ACEFD";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.BassSacdBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.BassSacdBehaviourConfiguration_Enabled))
                .WithElement(new SelectionConfigurationElement(AREA, Strings.BassSacdBehaviourConfiguration_Area).WithOptions(GetAreaOptions()).DependsOn(SECTION, ENABLED))
                .WithElement(new CommandConfigurationElement(CLEANUP_ELEMENT, Strings.BassSacdBehaviourConfiguration_Cleanup).WithHandler(() => SacdPlaylistItemFactory.Cleanup()).DependsOn(SECTION, ENABLED));
        }

        private static IEnumerable<SelectionConfigurationOption> GetAreaOptions()
        {
            yield return new SelectionConfigurationOption(AREA_STEREO, Strings.BassSacdBehaviourConfiguration_Area_Stereo).Default();
            yield return new SelectionConfigurationOption(AREA_MULTI_CHANNEL, Strings.BassSacdBehaviourConfiguration_Area_MultiChannel);
        }
    }
}
