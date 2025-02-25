using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassOutputConfiguration
    {
        public const string SECTION = "8399D051-81BC-41A6-940D-36E6764213D2";

        public const string RATE_ELEMENT = "AAAAA558-F1ED-41B1-A3DC-95158E01003C";

        public const string ENFORCE_RATE_ELEMENT = "JJJJ5B16-1B49-4C50-A8CF-BE3A6CCD4A87";

        public const string RESAMPLE_QUALITY_ELEMENT = "JJKKF36D-41BC-462F-8F8F-C3E6BD5A4661";

        public const string DEPTH_ELEMENT = "KKKKA2A6-DCA0-4E27-9812-498BB2A2C4BC";

        public const string DEPTH_16_OPTION = "LLLL8466-7582-4B1C-8687-7AB75D636CD8";

        public const string DEPTH_32_OPTION = "MMMM73F2-2E08-4F2B-B94E-DAA945D96497";

        public const string INPUT_ELEMENT = "MMNN52A1-79E9-43AA-BC17-C9DB335AFC9C";

        public const string OUTPUT_ELEMENT = "NNNN6B39-2F8A-4667-9C03-9742FF2D1EA7";

        public const string BUFFER_LENGTH_ELEMENT = "PPPP3629-1AE5-451F-A545-8B864FEAD038";

        public const string MIXER_BUFFER_LENGTH_ELEMENT = "PPPQ18BB-0485-4472-880D-3AB2ADFB1E42";

        public const string VOLUME_ENABLED_ELEMENT = "PPQQE42E-E530-4995-B3AC-CF6B2D9FE95B";

        public const string VOLUME_ELEMENT = "QQQQ1AF3-507A-426A-AE65-F118D1E71F2D";

        public const string DEVICE_MONITOR_ELEMENT = "RRRRDF0E-CC69-41E2-B149-DBCDDC2622EA";

        public const string UPDATE_PERIOD_ELEMENT = "SSSSE2CC-4971-4A32-8637-28026559A2C5";

        public const string UPDATE_THREADS_ELEMENT = "TTTT5DC2-19FE-4988-AD9C-8327B10A4763";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.BassOutputConfiguration_Section)
                .WithElement(new SelectionConfigurationElement(RATE_ELEMENT, Strings.BassOutputConfiguration_Rate, path: Strings.General_Advanced).WithOptions(GetRateOptions()))
                .WithElement(new SelectionConfigurationElement(DEPTH_ELEMENT, Strings.BassOutputConfiguration_Depth, path: Strings.General_Advanced).WithOptions(GetDepthOptions()))
                .WithElement(new SelectionConfigurationElement(INPUT_ELEMENT, Strings.BassOutputConfiguration_Input))
                .WithElement(new SelectionConfigurationElement(OUTPUT_ELEMENT, Strings.BassOutputConfiguration_Output))
                .WithElement(new BooleanConfigurationElement(ENFORCE_RATE_ELEMENT, Strings.BassOutputConfiguration_EnforceRate, path: Strings.General_Advanced).WithValue(false))
                .WithElement(new IntegerConfigurationElement(BUFFER_LENGTH_ELEMENT, Strings.BassOutputConfiguration_BufferLength, path: Strings.General_Advanced).WithValue(500).WithValidationRule(new IntegerValidationRule(10, 5000)))
                .WithElement(new IntegerConfigurationElement(MIXER_BUFFER_LENGTH_ELEMENT, Strings.BassOutputConfiguration_MixerBufferLength, path: Strings.General_Advanced).WithValue(2).WithValidationRule(new IntegerValidationRule(1, 5)))
                .WithElement(new BooleanConfigurationElement(VOLUME_ENABLED_ELEMENT, Strings.BassOutputConfiguration_Volume, path: Strings.General_Advanced).WithValue(Publication.ReleaseType == ReleaseType.Default))
                .WithElement(new DoubleConfigurationElement(VOLUME_ELEMENT, Strings.BassOutputConfiguration_VolumeLevel, path: Strings.General_Advanced).WithValue(1).WithValidationRule(new DoubleValidationRule(0, 1, 0.01)).DependsOn(SECTION, VOLUME_ENABLED_ELEMENT))
                .WithElement(new IntegerConfigurationElement(RESAMPLE_QUALITY_ELEMENT, Strings.BassOutputConfiguration_ResampleQuality, path: Strings.General_Advanced).WithValue(2).WithValidationRule(new IntegerValidationRule(1, 10)))
                .WithElement(new BooleanConfigurationElement(DEVICE_MONITOR_ELEMENT, Strings.BassOutputConfiguration_DeviceMonitor, path: Strings.General_Advanced).WithValue(Publication.ReleaseType == ReleaseType.Default))
                .WithElement(new IntegerConfigurationElement(UPDATE_PERIOD_ELEMENT, Strings.BassOutputConfiguration_UpdatePeriod, path: Strings.General_Advanced).WithValue(100).WithValidationRule(new IntegerValidationRule(5, 100)))
                .WithElement(new IntegerConfigurationElement(UPDATE_THREADS_ELEMENT, Strings.BassOutputConfiguration_UpdateThreads, path: Strings.General_Advanced).WithValue(1).WithValidationRule(new IntegerValidationRule(1, Environment.ProcessorCount))
            );
        }

        public static IEnumerable<SelectionConfigurationOption> GetRateOptions()
        {
            foreach (var rate in OutputRate.PCM)
            {
                yield return new SelectionConfigurationOption(rate.ToString(), rate.ToString());
            }
        }

        public static int GetRate(SelectionConfigurationOption option)
        {
            var rate = default(int);
            if (int.TryParse(option.Id, out rate))
            {
                return rate;
            }
            return OutputRate.PCM_44100;
        }

        public static IEnumerable<SelectionConfigurationOption> GetDepthOptions()
        {
            var i16 = new SelectionConfigurationOption(DEPTH_16_OPTION, "16bit");
            var f32 = new SelectionConfigurationOption(DEPTH_32_OPTION, "32bit floating point");
            if (Publication.ReleaseType == ReleaseType.Minimal)
            {
                i16.Default();
            }
            else
            {
                f32.Default();
            }
            yield return i16;
            yield return f32;
        }

        public static bool GetFloat(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                default:
                case DEPTH_16_OPTION:
                    return false;
                case DEPTH_32_OPTION:
                    return true;
            }
        }
    }
}
