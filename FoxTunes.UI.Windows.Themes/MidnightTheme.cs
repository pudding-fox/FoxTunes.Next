using System;
using System.IO;
using System.Windows;

namespace FoxTunes
{
    [Component(ID)]
    [WindowsUserInterfaceDependency]
    public class MidnightTheme : ThemeBase
    {
        public const string ID = "10D39C6C-8751-4D54-A93B-96D949867523";

        public MidnightTheme()
            : base(ID, Strings.MidnightTheme_Name, Strings.MidnightTheme_Description, global::FoxTunes.ColorPalettes.Dark)
        {

        }

        public override ResourceDictionary GetResourceDictionary()
        {
            return new ResourceDictionary()
            {
                Source = new Uri("/FoxTunes.UI.Windows.Themes;component/Themes/Midnight.xaml", UriKind.Relative)
            };
        }

        public override Stream GetArtworkPlaceholder()
        {
            return typeof(MidnightTheme).Assembly.GetManifestResourceStream("FoxTunes.UI.Windows.Themes.Images.Artwork.png");
        }
    }
}
