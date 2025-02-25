using FoxTunes.Interfaces;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class WindowExtensions
    {
        private static readonly ConditionalWeakTable<Window, FontSizeBehaviour> FontSizeBehaviours = new ConditionalWeakTable<Window, FontSizeBehaviour>();

        public static readonly DependencyProperty FontSizeProperty = DependencyProperty.RegisterAttached(
            "FontSize",
            typeof(bool),
            typeof(WindowExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnFontSizePropertyChanged))
        );

        public static bool GetFontSize(Window source)
        {
            return (bool)source.GetValue(FontSizeProperty);
        }

        public static void SetFontSize(Window source, bool value)
        {
            source.SetValue(FontSizeProperty, value);
        }

        private static void OnFontSizePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var window = sender as Window;
            if (window == null)
            {
                return;
            }
            if (GetFontSize(window))
            {
                var behaviour = default(FontSizeBehaviour);
                if (!FontSizeBehaviours.TryGetValue(window, out behaviour))
                {
                    FontSizeBehaviours.Add(window, new FontSizeBehaviour(window));
                }
            }
            else
            {
                FontSizeBehaviours.Remove(window);
            }
        }

        private class FontSizeBehaviour : UIBehaviour
        {
            public static bool Warned = false;

            private FontSizeBehaviour()
            {
                this.Configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();
            }

            public FontSizeBehaviour(Window window) : this()
            {
                this.Window = window;
                if (this.Configuration != null)
                {
                    this.Configuration.GetElement<DoubleConfigurationElement>(
                        WindowsUserInterfaceConfiguration.SECTION,
                        WindowsUserInterfaceConfiguration.FONT_SIZE
                    ).ConnectValue(value =>
                    {
                        this.FontSize = value;
                        if (this.Window != null)
                        {
                            this.EnableFontSize(value);
                        }
                    });
                }
            }

            public IConfiguration Configuration { get; private set; }

            public double FontSize { get; private set; }

            public Window Window { get; private set; }

            public virtual void EnableFontSize(double fontSize)
            {
                if (fontSize <= 0)
                {
                    this.Window.FontSize = SystemFonts.MessageFontSize;
                }
                else
                {
                    this.Window.FontSize = FontSize;
                }
            }
        }
    }
}
