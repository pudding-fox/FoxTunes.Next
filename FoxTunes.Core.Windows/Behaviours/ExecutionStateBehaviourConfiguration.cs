using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class ExecutionStateBehaviourConfiguration
    {
        public const string SECTION = "A01D8E9B-004B-48D5-B8A5-7ACCCC6D560F";

        public const string SLEEP_ELEMENT = "FBD451E4-6DC4-411E-A02C-0B41FB641778";

        public const string SLEEP_NONE_OPTION = "AAAAB02F3-19B1-432F-AA0F-EEDD04820CC2";

        public const string SLEEP_SYSTEM_OPTION = "BBBB4492-3D6B-47BC-84C3-945AFB2B12C9";

        public const string SLEEP_DISPLAY_OPTION = "CCCC64A7-1884-484F-AA13-7C221B1EA128";

        public const string ONLY_WHILE_PLAYING_ELEMENT = "GGGG5385-BECC-4111-894D-82A30A1AA3A0";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.ExecutionStateBehaviourConfiguration_Section)
                .WithElement(new SelectionConfigurationElement(SLEEP_ELEMENT, Strings.ExecutionStateBehaviourConfiguration_Sleep)
                    .WithOptions(GetSleepOptions()))
                .WithElement(new BooleanConfigurationElement(ONLY_WHILE_PLAYING_ELEMENT, Strings.ExecutionStateBehaviourConfiguration_OnlyWhilePlaying)
                    .WithValue(true)
                    .DependsOn(SECTION, SLEEP_ELEMENT, SLEEP_NONE_OPTION, true)
               );
        }

        private static IEnumerable<SelectionConfigurationOption> GetSleepOptions()
        {
            yield return new SelectionConfigurationOption(SLEEP_NONE_OPTION, Strings.ExecutionStateBehaviourConfiguration_SleepNone);
            yield return new SelectionConfigurationOption(SLEEP_SYSTEM_OPTION, Strings.ExecutionStateBehaviourConfiguration_SleepSystem);
            yield return new SelectionConfigurationOption(SLEEP_DISPLAY_OPTION, Strings.ExecutionStateBehaviourConfiguration_SleepDisplay);
        }

        public static EXECUTION_STATE GetExecutionState(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                default:
                case SLEEP_NONE_OPTION:
                    return EXECUTION_STATE.ES_CONTINUOUS;
                case SLEEP_SYSTEM_OPTION:
                    return EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED;
                case SLEEP_DISPLAY_OPTION:
                    return EXECUTION_STATE.ES_CONTINUOUS | EXECUTION_STATE.ES_SYSTEM_REQUIRED | EXECUTION_STATE.ES_DISPLAY_REQUIRED;
            }
        }
    }

    [Flags]
    public enum EXECUTION_STATE : uint
    {
        ES_AWAYMODE_REQUIRED = 0x00000040,
        ES_CONTINUOUS = 0x80000000,
        ES_DISPLAY_REQUIRED = 0x00000002,
        ES_SYSTEM_REQUIRED = 0x00000001
    }
}
