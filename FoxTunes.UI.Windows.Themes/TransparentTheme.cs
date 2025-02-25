using FoxDb;
using FoxTunes.Interfaces;
using FoxTunes.UI.Windows.Themes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace FoxTunes
{
    [Component(ID)]
    [WindowsUserInterfaceDependency]
    [ComponentPreference(ReleaseType.Default)]
    public class TransparentTheme : ThemeBase, IConfigurableComponent
    {
        public const string ID = "191C2E5B-4732-4CC7-BC31-4CC040DCFEC9";

        public TransparentTheme()
            : base(ID, Strings.TransparentTheme_Name, Strings.TransparentTheme_Description, global::FoxTunes.ColorPalettes.Transparent)
        {

        }

        public IntegerConfigurationElement Opacity { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.Opacity = this.Configuration.GetElement<IntegerConfigurationElement>(
                TransparentThemeConfiguration.SECTION,
                TransparentThemeConfiguration.OPACITY
            );
            this.Opacity.ValueChanged += this.OnValueChanged;
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            this.SetOpacity();
        }

        protected virtual void SetOpacity()
        {
            if (!this.ResourceDictionary.IsValueCreated)
            {
                return;
            }
            this.SetOpacity(this.ResourceDictionary.Value);
        }

        protected virtual void SetOpacity(ResourceDictionary resourceDictionary)
        {
            var names = new[]
            {
                "ControlBrush",
                "ControlBackgroundBrush",
                "ControlBorderBrush"
            };
            var opacity = (float)this.Opacity.Value / 100;
            foreach (var name in names)
            {
                if (resourceDictionary[name] is SolidColorBrush solidColorBrush)
                {
                    resourceDictionary[name] = new SolidColorBrush(solidColorBrush.Color).With(
                        brush => brush.Opacity = opacity
                    );
                }
            }
        }

        public override ResourceDictionary GetResourceDictionary()
        {
            var resourceDictionary = new ResourceDictionary()
            {
                Source = new Uri("/FoxTunes.UI.Windows.Themes;component/Themes/Transparent.xaml", UriKind.Relative)
            };
            this.SetOpacity(resourceDictionary);
            return resourceDictionary;
        }

        public override Stream GetArtworkPlaceholder()
        {
            return typeof(AdamantineTheme).Assembly.GetManifestResourceStream("FoxTunes.UI.Windows.Themes.Images.Artwork.png");
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return TransparentThemeConfiguration.GetConfigurationSections();
        }
    }
}
