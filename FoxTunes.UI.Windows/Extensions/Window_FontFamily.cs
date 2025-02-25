using FoxTunes.Interfaces;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class WindowExtensions
    {
        private static readonly ConditionalWeakTable<Window, FontFamilyBehaviour> FontFamilyBehaviours = new ConditionalWeakTable<Window, FontFamilyBehaviour>();

        public static readonly DependencyProperty FontFamilyProperty = DependencyProperty.RegisterAttached(
            "FontFamily",
            typeof(bool),
            typeof(WindowExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnFontFamilyPropertyChanged))
        );

        public static bool GetFontFamily(Window source)
        {
            return (bool)source.GetValue(FontFamilyProperty);
        }

        public static void SetFontFamily(Window source, bool value)
        {
            source.SetValue(FontFamilyProperty, value);
        }

        private static void OnFontFamilyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var window = sender as Window;
            if (window == null)
            {
                return;
            }
            if (GetFontFamily(window))
            {
                var behaviour = default(FontFamilyBehaviour);
                if (!FontFamilyBehaviours.TryGetValue(window, out behaviour))
                {
                    FontFamilyBehaviours.Add(window, new FontFamilyBehaviour(window));
                }
            }
            else
            {
                FontFamilyBehaviours.Remove(window);
            }
        }

        private class FontFamilyBehaviour : UIBehaviour
        {
            public static bool Warned = false;

            private FontFamilyBehaviour()
            {
                this.Configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();
            }

            public FontFamilyBehaviour(Window window) : this()
            {
                this.Window = window;
                if (this.Configuration != null)
                {
                    this.Configuration.GetElement<TextConfigurationElement>(
                        WindowsUserInterfaceConfiguration.SECTION,
                        WindowsUserInterfaceConfiguration.FONT_FAMILY
                    ).ConnectValue(value =>
                    {
                        this.FontFamily = value;
                        if (this.Window != null)
                        {
                            this.EnableFontFamily(value);
                        }
                    });
                }
            }

            public IConfiguration Configuration { get; private set; }

            public string FontFamily { get; private set; }

            public Window Window { get; private set; }

            public virtual void EnableFontFamily(string fontFamily)
            {
                if (string.IsNullOrEmpty(fontFamily))
                {
                    this.Window.FontFamily = SystemFonts.MessageFontFamily;
                }
                else
                {
                    this.Window.FontFamily = new FontFamily(fontFamily);
                }
            }
        }
    }
}
