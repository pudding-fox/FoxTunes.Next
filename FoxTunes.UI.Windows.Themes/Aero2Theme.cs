using System;
using System.IO;
using System.Windows;

namespace FoxTunes
{
    [Component(ID)]
    [WindowsUserInterfaceDependency]
    public class Aero2Theme : ThemeBase
    {
        public const string ID = "FB90B62E-839D-43B3-B9C2-DBFF498BFE82";

        public Aero2Theme() : base(ID, Strings.Aero2Theme_Name, Strings.Aero2Theme_Description, global::FoxTunes.ColorPalettes.Light)
        {

        }

        public override ResourceDictionary GetResourceDictionary()
        {
            var resourceDictionary = new ResourceDictionary();
            resourceDictionary.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("/PresentationFramework.Aero2,Version=4.0.0.0,Culture=neutral,PublicKeyToken=31bf3856ad364e35,processorArchitecture=MSIL;component/themes/Aero2.NormalColor.xaml", UriKind.Relative)
            });
            resourceDictionary.MergedDictionaries.Add(new ResourceDictionary()
            {
                Source = new Uri("/FoxTunes.UI.Windows.Themes;component/Themes/Aero2.xaml", UriKind.Relative)
            });
            return resourceDictionary;
        }

        public override Stream GetArtworkPlaceholder()
        {
            return typeof(Aero2Theme).Assembly.GetManifestResourceStream("FoxTunes.UI.Windows.Themes.Images.Artwork.png");
        }
    }
}
