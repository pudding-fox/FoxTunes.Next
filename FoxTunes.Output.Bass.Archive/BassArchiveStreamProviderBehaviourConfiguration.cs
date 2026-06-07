using ManagedBass.ZipStream;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassArchiveStreamProviderBehaviourConfiguration
    {
        public const string SECTION = "B593BE99-CCA4-42D2-A129-7F65A96FD302";

        public const string ENABLED = "AAAA73E0-DAAD-4B73-A375-D6D820AFAE93";

        public const string METADATA = "BBBB68A8-AC18-4FBF-8C87-DE24EA49262C";

        public const string BUFFER_MIN = "CCCCB244-66D0-44EE-95CB-52DA64D6C597";

        public const string BUFFER_TIMEOUT = "DDDD48A8-E758-4819-B2FB-4F96879EC018";

        public const string DOUBLE_BUFFER = "EEEE6A31-93C1-49EC-BE01-CF27553D9DE6";

        public const string CLEANUP = "ZZZZB7CC-C87C-4A96-96A2-F52D7495D4D4";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.BassArchiveStreamProviderBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.BassArchiveStreamProviderBehaviourConfiguration_Enabled)
                    .WithValue(true))
                .WithElement(new BooleanConfigurationElement(METADATA, Strings.BassArchiveStreamProviderBehaviourConfiguration_MetaData)
                    .WithValue(true)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new IntegerConfigurationElement(BUFFER_MIN, Strings.BassArchiveStreamProviderBehaviourConfiguration_BufferMin)
                    .WithValue(BassZipStream.DEFAULT_BUFFER_MIN)
                    .WithValidationRule(new IntegerValidationRule(0, 100))
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new IntegerConfigurationElement(BUFFER_TIMEOUT, Strings.BassArchiveStreamProviderBehaviourConfiguration_BufferTimeout)
                    .WithValue(BassZipStream.DEFAULT_BUFFER_TIMEOUT)
                    .WithValidationRule(new IntegerValidationRule(1, 5000))
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new BooleanConfigurationElement(DOUBLE_BUFFER, Strings.BassArchiveStreamProviderBehaviourConfiguration_DoubleBuffer)
                    .WithValue(BassZipStream.DEFAULT_DOUBLE_BUFFER)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new CommandConfigurationElement(CLEANUP, Strings.BassArchiveStreamProviderBehaviourConfiguration_Cleanup)
                    .WithHandler(() => Archive.Cleanup())
                    .DependsOn(SECTION, ENABLED));
        }
    }
}
