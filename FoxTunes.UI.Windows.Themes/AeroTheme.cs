using System;
using System.IO;
using System.Windows;

namespace FoxTunes
{
    [Component(ID)]
    [WindowsUserInterfaceDependency]
    public class AeroTheme : ThemeBase
    {
        public const string ID = "99941BF7-D348-493B-9E4E-3CAC71C562B6";

        public AeroTheme() : base(ID, Strings.AeroTheme_Name, Strings.AeroTheme_Description, global::FoxTunes.ColorPalettes.Light)
        {

        }

        public override ResourceDictionary GetResourceDictionary()
        {
            var resourceDictionary = new ResourceDictionary();
            resourceDictionary.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("/PresentationFramework.Aero,Version=4.0.0.0,Culture=neutral,PublicKeyToken=31bf3856ad364e35,processorArchitecture=MSIL;component/themes/Aero.NormalColor.xaml", UriKind.Relative)
            });
            resourceDictionary.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("/FoxTunes.UI.Windows.Themes;component/Themes/Aero.xaml", UriKind.Relative)
            });
            return resourceDictionary;
        }

        public override Stream GetArtworkPlaceholder()
        {
            return typeof(AeroTheme).Assembly.GetManifestResourceStream("FoxTunes.UI.Windows.Themes.Images.Artwork.png");
        }
    }
}
