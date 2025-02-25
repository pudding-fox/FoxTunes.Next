using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    //Requires Windows 11 22H2.
    [PlatformDependency(Major = 6, Minor = 2, Build = 22621)]
    public class WindowAcrylicBlurBehaviour : WindowBlurProvider
    {
        public const string ID = "BBBBC45C-11A4-4A2A-83F2-4FFED3C72C3E";

        public override string Id
        {
            get
            {
                return ID;
            }
        }

        public TextConfigurationElement AccentColor { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.AccentColor = this.Configuration.GetElement<TextConfigurationElement>(
                WindowAcrylicBlurBehaviourConfiguration.SECTION,
                WindowAcrylicBlurBehaviourConfiguration.ACCENT_COLOR
            );
            this.AccentColor.ValueChanged += this.OnValueChanged;
        }

        protected override void OnRefresh()
        {
            var windows = new HashSet<IntPtr>();
            foreach (var window in WindowBase.Active)
            {
                windows.Add(window.Handle);
                WindowExtensions.EnableAcrylicBlur(
                    window.Handle,
                    this.GetAccentColor()
                );
            }
        }

        protected virtual Color GetAccentColor()
        {
            if (!string.IsNullOrEmpty(this.AccentColor.Value))
            {
                var color = this.AccentColor.Value.ToColor();
                return Color.FromArgb(
                    WindowExtensions.DefaultAccentColor.A,
                    color.R,
                    color.G,
                    color.B
                );
            }
            else
            {
                return WindowExtensions.DefaultAccentColor;
            }
        }

        public override IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return WindowAcrylicBlurBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
