using System.Collections.Generic;

namespace FoxTunes
{
    public static class PlaybackStatisticsBehaviourConfiguration
    {
        public const string SECTION = PlaybackBehaviourConfiguration.SECTION;

        public const string ENABLED = "2B879758-2672-4513-A348-FDD08E1E8500";

        public const string TRIGGER = "AAAA862B-6CEC-4074-A089-F12840D25870";

        public const string TRIGGER_BEGIN = "AAAAFF8F-86BC-4F60-A102-6024F649C10B";

        public const string TRIGGER_END = "BBBB4601-64D8-4BB2-9397-33270443AC9F";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.PlaybackStatisticsBehaviourConfiguration_Enabled)
                    .WithValue(Publication.ReleaseType == ReleaseType.Default))
                .WithElement(new SelectionConfigurationElement(TRIGGER, Strings.PlaybackStatisticsBehaviourConfiguration_Trigger)
                    .WithOptions(GetTriggerOptions())
                    .DependsOn(SECTION, ENABLED)
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetTriggerOptions()
        {
            yield return new SelectionConfigurationOption(TRIGGER_BEGIN, Strings.PlaybackStatisticsBehaviourConfiguration_Trigger_Begin);
            yield return new SelectionConfigurationOption(TRIGGER_END, Strings.PlaybackStatisticsBehaviourConfiguration_Trigger_End);
        }

        public static UpdatePlaybackStatisticsTrigger GetTrigger(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                default:
                case TRIGGER_BEGIN:
                    return UpdatePlaybackStatisticsTrigger.Begin;
                case TRIGGER_END:
                    return UpdatePlaybackStatisticsTrigger.End;
            }
        }
    }

    public enum UpdatePlaybackStatisticsTrigger : byte
    {
        None,
        Begin,
        End
    }
}
