using System.Collections.Generic;
using Windows.UI.Notifications;

namespace FoxTunes
{
    public static class ToastNotificationManagerBehaviourConfiguration
    {
        public const string SECTION = UWPConfiguration.SECTION;

        public const string ENABLED_ELEMENT = "AAAAD7EF-DFBF-4633-8B92-1F0047D3E9CC";

        public const string POPUP_ELEMENT = "BBBB8A9D-260D-4757-8D61-36E50BF8D3E0";

        public const string LARGE_ARTWORK_ELEMENT = "CCCCC054-BFF2-48BD-B2D9-7471D003A4E3";

        /// <summary>
        /// Although toast notifications are supposed to be a Windows 10 feature it's actually available on 2016.
        /// This means a check of Environment.OSVersion isn't appropriate.
        /// Due to the way the UWP works we need to indirect the reference to ToastNotificationManager.
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
                var types = new[] { typeof(ToastNotificationManager) };
#pragma warning restore CS0618
                return true;
            }
        }

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            if (IsPlatformSupported)
            {
                yield return new ConfigurationSection(SECTION, "Windows 10")
                    .WithElement(new BooleanConfigurationElement(ENABLED_ELEMENT, "Notifications").WithValue(Publication.ReleaseType == ReleaseType.Default))
                    .WithElement(new BooleanConfigurationElement(POPUP_ELEMENT, "Popup").WithValue(false).DependsOn(SECTION, ENABLED_ELEMENT))
                    .WithElement(new BooleanConfigurationElement(LARGE_ARTWORK_ELEMENT, "Large Artwork").WithValue(false).DependsOn(SECTION, ENABLED_ELEMENT)
                );
            }
        }
    }
}
