using System.Collections.Generic;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    //TODO: Not sure of the exact required platform.
    //TODO: SetWindowCompositionAttribute is undocumented.
    //TODO: Assuming Windows 8.
    [PlatformDependency(Major = 6, Minor = 2)]
    public class WindowBlurBehaviour : WindowBlurProvider
    {
        public const string ID = "AAAA8904-827D-449F-A69C-EA57C17852BC";

        public override string Id
        {
            get
            {
                return ID;
            }
        }

        protected override void OnRefresh()
        {
            foreach (var window in WindowBase.Active)
            {
                WindowExtensions.EnableBlur(window.Handle);
            }
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return WindowBlurBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
