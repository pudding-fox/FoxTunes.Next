using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassReplayGainScannerBehaviourConfiguration
    {
        public const string SECTION = BassOutputConfiguration.SECTION;

        public const string ENABLED = BassReplayGainBehaviourConfiguration.ENABLED;

        public const string WRITE_TAGS = "AAAA2157-6395-40A7-80DE-C2DCDE4B5131";

        public const string THREADS = "BBBB7889-4D4C-4EB8-8EE9-BE075B64347A";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(
                    new BooleanConfigurationElement(WRITE_TAGS, Strings.BassReplayGainScannerBehaviourConfiguration_WriteTags, path: Strings.BassReplayGainScannerBehaviourConfiguration_Path)
                        .WithValue(true)
                        .DependsOn(SECTION, ENABLED))
                .WithElement(
                    new IntegerConfigurationElement(THREADS, Strings.BassReplayGainScannerBehaviourConfiguration_BackgroundThreads, path: Strings.BassReplayGainScannerBehaviourConfiguration_Path)
                        .WithValue(Environment.ProcessorCount).WithValidationRule(new IntegerValidationRule(1, 32))
                        .DependsOn(SECTION, ENABLED)
                );
        }
    }
}
