using System;
using System.IO;
using System.Windows;

namespace FoxTunes
{
    [Component(ID)]
    [WindowsUserInterfaceDependency]
    public class FoxTheme : ThemeBase
    {
        public const string ID = "22921443-93C6-42CE-9DB8-D3B7D0015069";

        public FoxTheme()
            : base(ID, Strings.FoxTheme_Name, Strings.FoxTheme_Description, global::FoxTunes.ColorPalettes.Fox)
        {

        }

        public override ResourceDictionary GetResourceDictionary()
        {
            return new ResourceDictionary()
            {
                Source = new Uri("/FoxTunes.UI.Windows.Themes;component/Themes/Fox.xaml", UriKind.Relative)
            };
        }

        public override Stream GetArtworkPlaceholder()
        {
            return typeof(AdamantineTheme).Assembly.GetManifestResourceStream("FoxTunes.UI.Windows.Themes.Images.Fox.png");
        }
    }
}
