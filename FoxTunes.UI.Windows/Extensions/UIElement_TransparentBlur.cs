using FoxTunes.Interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media.Effects;

namespace FoxTunes
{
    public static partial class UIElementExtensions
    {
        private static readonly ConditionalWeakTable<UIElement, TransparentBlurBehaviour> TransparentBlurBehaviours = new ConditionalWeakTable<UIElement, TransparentBlurBehaviour>();

        public static readonly DependencyProperty TransparentBlurProperty = DependencyProperty.RegisterAttached(
            "TransparentBlur",
            typeof(bool),
            typeof(UIElementExtensions),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnTransparentBlurPropertyChanged))
        );

        public static bool GetTransparentBlur(UIElement source)
        {
            return (bool)source.GetValue(TransparentBlurProperty);
        }

        public static void SetTransparentBlur(UIElement source, bool value)
        {
            source.SetValue(TransparentBlurProperty, value);
        }

        private static void OnTransparentBlurPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var element = sender as UIElement;
            if (element == null)
            {
                return;
            }
            var behaviour = default(TransparentBlurBehaviour);
            if (TransparentBlurBehaviours.TryRemove(element, out behaviour))
            {
                behaviour.Dispose();
            }
            if (GetTransparentBlur(element))
            {
                TransparentBlurBehaviours.Add(element, new TransparentBlurBehaviour(element));
            }
        }

        private class TransparentBlurBehaviour : UIBehaviour
        {
            private TransparentBlurBehaviour()
            {
                this.Configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();
            }

            public TransparentBlurBehaviour(UIElement element) : this()
            {
                this.Element = element;
                if (this.Configuration != null)
                {
                    this.Configuration.GetElement<BooleanConfigurationElement>(
                        WindowsUserInterfaceConfiguration.SECTION,
                        WindowsUserInterfaceConfiguration.TRANSPARENCY
                    ).ConnectValue(value =>
                    {
                        if (value)
                        {
                            this.Element.Effect = new BlurEffect()
                            {
                                Radius = 20
                            };
                        }
                        else
                        {
                            this.Element.Effect = null;
                        }
                    });
                }
            }

            public IConfiguration Configuration { get; private set; }

            public UIElement Element { get; private set; }

            protected override void OnDisposing()
            {
                this.Element.Effect = null;
                base.OnDisposing();
            }
        }
    }
}
