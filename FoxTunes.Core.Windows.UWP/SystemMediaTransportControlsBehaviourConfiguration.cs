using System.Collections.Generic;
using Windows.Media;
using Windows.Media.Playback;

namespace FoxTunes
{
    public static class SystemMediaTransportControlsBehaviourConfiguration
    {
        public const string SECTION = UWPConfiguration.SECTION;

        public const string ENABLED_ELEMENT = "AAAA69E1-80B1-46BD-BE24-BC56C5A04141";

        /// <summary>
        /// Although the system media transport controls are supposed to be a Windows 10 feature it's actually available on 2016.
        /// This means a check of Environment.OSVersion isn't appropriate.
        /// Due to the way the UWP works we need to indirect the reference to SystemMediaTransportControls.
        /// </summary>
        public static bool IsPlatformSupported
        {
            get
            {
                try
                {
                    return _IsPlatformSupported;
                }
                catch
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Accessing this property will raise an exception if the platform is not supported.
        /// </summary>
        private static bool _IsPlatformSupported
        {
            get
            {
#pragma warning disable CS0618
                var types = new[] { typeof(BackgroundMediaPlayer), typeof(SystemMediaTransportControls) };
#pragma warning restore CS0618
                return true;
            }
        }

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            if (IsPlatformSupported)
            {
                yield return new ConfigurationSection(SECTION, "Windows 10")
                    .WithElement(new BooleanConfigurationElement(ENABLED_ELEMENT, "System Media Controls").WithValue(Publication.ReleaseType == ReleaseType.Default)
                );
            }
        }
    }
}
