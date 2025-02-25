using ManagedBass.Sox;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassResamplerStreamComponentConfiguration
    {
        public const string SECTION = BassOutputConfiguration.SECTION;

        public const string ENABLED_ELEMENT = "AAAA5C85-178C-470D-A977-C54350875AB3";

        public const string QUALITY_ELEMENT = "BBBB0ED2-67E6-4155-AFB0-FE8D7E7B9B8C";

        public const string PHASE_ELEMENT = "CCCC4941-E1DC-4C2E-9DF7-14C1638C392F";

        public const string STEEP_FILTER_ELEMENT = "DDDD0064-06D7-4EFA-B3C9-2820B8893039";

        public const string INPUT_BUFFER_LENGTH = "EEFF4B8B1A-4B97-4971-AA4D-40ACC2C68828";

        public const string PLAYBACK_BUFFER_LENGTH_ELEMENT = "FFFF63F8-2695-4853-89E8-ADC10D8EFC2E";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new BooleanConfigurationElement(ENABLED_ELEMENT, Strings.BassResamplerStreamComponentConfiguration_Enabled, path: Strings.BassResamplerStreamComponentConfiguration_Path).WithValue(false))
                .WithElement(new SelectionConfigurationElement(QUALITY_ELEMENT, Strings.BassResamplerStreamComponentConfiguration_Quality, path: Strings.BassResamplerStreamComponentConfiguration_Path).WithOptions(GetQualityOptions()).DependsOn(BassOutputConfiguration.SECTION, ENABLED_ELEMENT))
                .WithElement(new SelectionConfigurationElement(PHASE_ELEMENT, Strings.BassResamplerStreamComponentConfiguration_Phase, path: Strings.BassResamplerStreamComponentConfiguration_Path).WithOptions(GetPhaseOptions()).DependsOn(BassOutputConfiguration.SECTION, ENABLED_ELEMENT))
                .WithElement(new BooleanConfigurationElement(STEEP_FILTER_ELEMENT, Strings.BassResamplerStreamComponentConfiguration_SteepFilter, path: Strings.BassResamplerStreamComponentConfiguration_Path).WithValue(false).DependsOn(BassOutputConfiguration.SECTION, ENABLED_ELEMENT))
                .WithElement(new IntegerConfigurationElement(INPUT_BUFFER_LENGTH, Strings.BassResamplerStreamComponentConfiguration_InputBufferLength, path: Strings.BassResamplerStreamComponentConfiguration_Path).WithValue(100).WithValidationRule(new IntegerValidationRule(10, 1000)).DependsOn(BassOutputConfiguration.SECTION, ENABLED_ELEMENT))
                .WithElement(new IntegerConfigurationElement(PLAYBACK_BUFFER_LENGTH_ELEMENT, Strings.BassResamplerStreamComponentConfiguration_PlaybackBufferLength, path: Strings.BassResamplerStreamComponentConfiguration_Path).WithValue(300).WithValidationRule(new IntegerValidationRule(100, 10000)).DependsOn(BassOutputConfiguration.SECTION, ENABLED_ELEMENT)
            );
        }

        public static SoxChannelQuality GetQuality(SelectionConfigurationOption option)
        {
            var quality = default(SoxChannelQuality);
            if (!Enum.TryParse<SoxChannelQuality>(option.Id, out quality))
            {
                return SoxChannelQuality.Medium;
            }
            return quality;
        }

        private static IEnumerable<SelectionConfigurationOption> GetQualityOptions()
        {
            var values = new[]
            {
                SoxChannelQuality.Quick,
                SoxChannelQuality.Low,
                SoxChannelQuality.Medium,
                SoxChannelQuality.High,
                SoxChannelQuality.VeryHigh,
                SoxChannelQuality.Ultra
            };
            foreach (var value in values)
            {
                var option = new SelectionConfigurationOption(
                    Enum.GetName(typeof(SoxChannelQuality), value),
                    Enum.GetName(typeof(SoxChannelQuality), value)
                );
                if (SoxChannelQuality.Medium.Equals(value))
                {
                    option.Default();
                }
                yield return option;
            }
        }

        public static SoxChannelPhase GetPhase(SelectionConfigurationOption option)
        {
            var quality = default(SoxChannelPhase);
            if (!Enum.TryParse<SoxChannelPhase>(option.Id, out quality))
            {
                return SoxChannelPhase.Linear;
            }
            return quality;
        }

        private static IEnumerable<SelectionConfigurationOption> GetPhaseOptions()
        {
            var values = Enum.GetValues(typeof(SoxChannelPhase));
            foreach (var value in values)
            {
                var option = new SelectionConfigurationOption(
                    Enum.GetName(typeof(SoxChannelPhase), value),
                    Enum.GetName(typeof(SoxChannelPhase), value)
                );
                if (SoxChannelPhase.Linear.Equals(value))
                {
                    option.Default();
                }
                yield return option;
            }
        }
    }
}
