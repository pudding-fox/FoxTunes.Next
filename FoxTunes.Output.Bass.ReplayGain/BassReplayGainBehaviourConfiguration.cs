using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassReplayGainBehaviourConfiguration
    {
        public const string SECTION = BassOutputConfiguration.SECTION;

        public const string ENABLED = "AAAA1379-60E3-426D-9CF0-61F11343A627";

        public const string ON_DEMAND = "AABB8343-832C-4B99-A34C-8D9475D56722";

        public const string MODE = "BBBB9149-D485-45F3-A505-750774C06D0D";

        public const string MODE_ALBUM = "AAAAF2BB-3753-48A2-A73F-EA46EAA0E91E";

        public const string MODE_TRACK = "BBBBEEB5-A4EC-45A0-8717-E7E8F1EF457E";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            var section = new ConfigurationSection(SECTION)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.BassReplayGainBehaviourConfiguration_Enabled, path: Strings.BassReplayGainBehaviourConfiguration_Path).WithValue(false))
                .WithElement(new SelectionConfigurationElement(MODE, Strings.BassReplayGainBehaviourConfiguration_Mode, path: Strings.BassReplayGainBehaviourConfiguration_Path).WithOptions(GetModeOptions()).DependsOn(BassOutputConfiguration.SECTION, ENABLED))
                .WithElement(new BooleanConfigurationElement(ON_DEMAND, Strings.BassReplayGainBehaviourConfiguration_OnDemand, path: Strings.BassReplayGainBehaviourConfiguration_Path).WithValue(false).DependsOn(BassOutputConfiguration.SECTION, ENABLED)
            );
            yield return section;
        }

        private static IEnumerable<SelectionConfigurationOption> GetModeOptions()
        {
            yield return new SelectionConfigurationOption(MODE_ALBUM, Strings.BassReplayGainBehaviourConfiguration_PreferAlbum).Default();
            yield return new SelectionConfigurationOption(MODE_TRACK, Strings.BassReplayGainBehaviourConfiguration_PreferTrack);
        }

        public static ReplayGainMode GetMode(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                default:
                case MODE_ALBUM:
                    return ReplayGainMode.Album;
                case MODE_TRACK:
                    return ReplayGainMode.Track;
            }
        }
    }

    public enum ReplayGainMode : byte
    {
        None,
        Album,
        Track
    }
}
