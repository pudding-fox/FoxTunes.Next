using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class LoggingBehaviourConfiguration
    {
        public const string SECTION = "6D2403BD-7CBD-4690-A119-90F4A45B00CD";

        public const string ENABLED_ELEMENT = "AAAA5DDB-106C-4C5F-9262-58396C18DDCC";

        public const string LEVEL_ELEMENT = "BBBB16BC-719C-4786-BB34-E2EE4F5C6491";

        public const string FATAL_OPTION = "AAAA1EBB-289E-413F-A990-70691184829B";

        public const string ERROR_OPTION = "BBBB49AB-893B-40AF-A074-3914000D12BE";

        public const string WARN_OPTION = "CCCC9743-DDC4-47FB-A39D-2081EDDD9A86";

        public const string INFO_OPTION = "DDDDCD06-9AE6-43E0-AA49-3AB1FFA01F20";

        public const string DEBUG_OPTION = "EEEE9A6E-65F9-4465-B7E9-418AD53F540B";

        public const string TRACE_OPTION = "FFFF9D19-17DE-40FC-A8DB-C2D5407F12BD";

        public const string OPEN_ELEMENT = "CCCC7035-7960-472E-A6E8-58F6AA0918F1";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
#if DEBUG
            var enabled = true;
#else
            var enabled = false;
#endif
            yield return new ConfigurationSection(SECTION, Strings.LoggingBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(ENABLED_ELEMENT, Strings.LoggingBehaviourConfiguration_Enabled)
                    .WithValue(enabled))
                .WithElement(new SelectionConfigurationElement(LEVEL_ELEMENT, Strings.LoggingBehaviourConfiguration_Level)
                    .WithOptions(GetLevelOptions())
                    .DependsOn(SECTION, ENABLED_ELEMENT))
                .WithElement(new CommandConfigurationElement(OPEN_ELEMENT, Strings.LoggingBehaviourConfiguration_Open)
                    .WithAsyncHandler(LogManager.Open)
                    .DependsOn(SECTION, ENABLED_ELEMENT));
        }

        private static IEnumerable<SelectionConfigurationOption> GetLevelOptions()
        {
            yield return new SelectionConfigurationOption(FATAL_OPTION, Strings.LoggingBehaviourConfiguration_Level_Fatal);
#if DEBUG
            yield return new SelectionConfigurationOption(ERROR_OPTION, Strings.LoggingBehaviourConfiguration_Level_Error);
#else
            yield return new SelectionConfigurationOption(ERROR_OPTION, Strings.LoggingBehaviourConfiguration_Level_Error).Default();
#endif
            yield return new SelectionConfigurationOption(WARN_OPTION, Strings.LoggingBehaviourConfiguration_Level_Warn);
            yield return new SelectionConfigurationOption(INFO_OPTION, Strings.LoggingBehaviourConfiguration_Level_Info);
            yield return new SelectionConfigurationOption(DEBUG_OPTION, Strings.LoggingBehaviourConfiguration_Level_Debug);
#if DEBUG
            yield return new SelectionConfigurationOption(TRACE_OPTION, Strings.LoggingBehaviourConfiguration_Level_Trace).Default();
#else
            yield return new SelectionConfigurationOption(TRACE_OPTION, Strings.LoggingBehaviourConfiguration_Level_Trace);
#endif
        }

        public static LogLevel GetLogLevel(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                case FATAL_OPTION:
                    return LogLevel.Fatal;
                default:
                case ERROR_OPTION:
                    return LogLevel.Error;
                case WARN_OPTION:
                    return LogLevel.Warn;
                case INFO_OPTION:
                    return LogLevel.Info;
                case DEBUG_OPTION:
                    return LogLevel.Debug;
                case TRACE_OPTION:
                    return LogLevel.Trace;
            }
        }
    }
}
