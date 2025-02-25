using System;
using System.IO;
using System.Windows;

namespace FoxTunes
{
    [Component(ID)]
    [WindowsUserInterfaceDependency]
    public class AdamantineTheme : ThemeBase
    {
        public const string ID = "06464CF4-118F-47EA-9597-303D305EF847";

        public AdamantineTheme()
            : base(ID, Strings.AdamantineTheme_Name, Strings.AdamantineTheme_Description, global::FoxTunes.ColorPalettes.Dark)
        {

        }

        public override ResourceDictionary GetResourceDictionary()
        {
            return new ResourceDictionary()
            {
                Source = new Uri("/FoxTunes.UI.Windows.Themes;component/Themes/Adamantine.xaml", UriKind.Relative)
            };
        }

        public override Stream GetArtworkPlaceholder()
        {
            return typeof(AdamantineTheme).Assembly.GetManifestResourceStream("FoxTunes.UI.Windows.Themes.Images.Artwork.png");
        }
    }
}
