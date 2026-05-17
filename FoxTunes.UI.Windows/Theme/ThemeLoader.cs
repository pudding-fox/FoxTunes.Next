using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class ThemeLoader : StandardComponent
    {
        public const string SELECT_COLOR_PALETTE_AUTO = "ZZZZ";

        public ITheme Theme { get; private set; }

        protected virtual void SetTheme(Func<ITheme> factory)
        {
            this.OnBeginSetTheme();
            try
            {
                if (this.Theme != null)
                {
                    this.Theme.Disable();
                }
                this.Theme = factory();
                this.Theme.Enable();
            }
            finally
            {
                this.OnEndSetTheme();
            }
            this.OnThemeChanged();
        }

        protected virtual void OnThemeChanged()
        {
            if (this.ThemeChanged != null)
            {
                this.ThemeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Theme");
        }

        public event EventHandler ThemeChanged;

        protected virtual void OnBeginSetTheme()
        {
            Application.Current.Resources.BeginInit();
        }

        protected virtual void OnEndSetTheme()
        {
            Application.Current.Resources.EndInit();
        }

        public IConfigurationBase Configuration { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<SelectionConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.THEME
            ).ConnectValue(value => this.SetTheme(() => WindowsUserInterfaceConfiguration.GetTheme(value)));
            base.InitializeComponent(core);
        }

        public Task EnsureTheme()
        {
            return Windows.Invoke(() =>
            {
                var theme = this.Theme;
            });
        }

        public ThemeLoader ConnectTheme(Action<ITheme> action)
        {
            var handler = new EventHandler((sender, e) =>
            {
                try
                {
                    action(this.Theme);
                }
                catch
                {
                    //TODO: Warn.
                }
            });
            handler(this, EventArgs.Empty);
            this.ThemeChanged += handler;
            return this;
        }

        public IEnumerable<IInvocationComponent> SelectColorPalette(string category, TextConfigurationElement element, ColorPaletteRole role)
        {
            return this.SelectColorPalette(category, element, colorPalette => colorPalette.Role.HasFlag(role));
        }

        public IEnumerable<IInvocationComponent> SelectColorPalette(string category, TextConfigurationElement element, Func<IColorPalette, bool> predicate)
        {
            foreach (var colorPalette in this.Theme.ColorPalettes)
            {
                if (colorPalette.Flags.HasFlag(ColorPaletteFlags.System))
                {
                    continue;
                }
                if (predicate != null && !predicate(colorPalette))
                {
                    continue;
                }
                yield return new InvocationComponent(
                    category,
                    colorPalette.Id,
                    colorPalette.Name,
                    path: element.Name,
                    attributes: string.Equals(colorPalette.Value, element.Value, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
            }
            yield return new InvocationComponent(
                category,
                SELECT_COLOR_PALETTE_AUTO,
                Strings.ThemeLoader_ColorPaletteAuto,
                path: element.Name,
                attributes: (byte)((string.IsNullOrEmpty(element.Value) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE) | InvocationComponent.ATTRIBUTE_SEPARATOR)
            );
        }

        public bool SelectColorPalette(TextConfigurationElement element, IInvocationComponent component)
        {
            if (string.Equals(element.Name, component.Path, StringComparison.OrdinalIgnoreCase))
            {
                foreach (var colorPalette in this.Theme.ColorPalettes)
                {
                    if (!string.Equals(colorPalette.Id, component.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                    element.Value = colorPalette.Value;
                    return true;
                }
                if (string.Equals(component.Id, SELECT_COLOR_PALETTE_AUTO, StringComparison.OrdinalIgnoreCase))
                {
                    element.Value = string.Empty;
                    return true;
                }
            }
            return false;
        }
    }
}
