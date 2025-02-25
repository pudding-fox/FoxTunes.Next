using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace FoxTunes
{
    [Component(ID)]
    [WindowsUserInterfaceDependency]
    public class ExpressionDarkTheme : ThemeBase
    {
        public const string ID = "3E9EFE8C-5245-4F8B-97D1-EB47CC70E373";

        public ExpressionDarkTheme()
            : base(ID, Strings.ExpressionDarkTheme_Name, Strings.ExpressionDarkTheme_Description, global::FoxTunes.ColorPalettes.Dark)
        {

        }

        public override ResourceDictionary GetResourceDictionary()
        {
            return new ResourceDictionary()
            {
                Source = new Uri("/FoxTunes.UI.Windows.Themes;component/Themes/ExpressionDark.xaml", UriKind.Relative)
            };
        }

        public override Stream GetArtworkPlaceholder()
        {
            return typeof(ExpressionDarkTheme).Assembly.GetManifestResourceStream("FoxTunes.UI.Windows.Themes.Images.Artwork.png");
        }
    }
}
