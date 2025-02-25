using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Interop;

namespace FoxTunes
{
    public static partial class PopupExtensions
    {
        private static readonly ConditionalWeakTable<global::System.Windows.Controls.Primitives.Popup, AllowsTransparencyBehaviour> AllowsTransparencyBehaviours = new ConditionalWeakTable<global::System.Windows.Controls.Primitives.Popup, AllowsTransparencyBehaviour>();

        public static readonly DependencyProperty AllowsTransparencyProperty = DependencyProperty.RegisterAttached(
            "AllowsTransparency",
            typeof(bool),
            typeof(PopupExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnAllowsTransparencyPropertyChanged))
        );

        public static bool GetAllowsTransparency(global::System.Windows.Controls.Primitives.Popup source)
        {
            return (bool)source.GetValue(AllowsTransparencyProperty);
        }

        public static void SetAllowsTransparency(global::System.Windows.Controls.Primitives.Popup source, bool value)
        {
            source.SetValue(AllowsTransparencyProperty, value);
        }

        private static void OnAllowsTransparencyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var popup = sender as global::System.Windows.Controls.Primitives.Popup;
            if (popup == null)
            {
                return;
            }
            if (GetAllowsTransparency(popup))
            {
                var behaviour = default(AllowsTransparencyBehaviour);
                if (!AllowsTransparencyBehaviours.TryGetValue(popup, out behaviour))
                {
                    AllowsTransparencyBehaviours.Add(popup, new AllowsTransparencyBehaviour(popup));
                }
            }
            else
            {
                AllowsTransparencyBehaviours.Remove(popup);
            }
        }

        private class AllowsTransparencyBehaviour : UIBehaviour<global::System.Windows.Controls.Primitives.Popup>
        {
            public AllowsTransparencyBehaviour(global::System.Windows.Controls.Primitives.Popup popup) : base(popup)
            {
                this.Popup = popup;
                this.Popup.Opened += this.OnOpened;
            }


            public global::System.Windows.Controls.Primitives.Popup Popup { get; private set; }

            protected virtual void OnOpened(object sender, EventArgs e)
            {
                var source = (HwndSource)HwndSource.FromVisual(this.Popup.Child);
                WindowExtensions.EnableBlur(source.Handle);
            }

            protected override void OnDisposing()
            {
                if (this.Popup != null)
                {
                    this.Popup.Opened += this.OnOpened;
                }
                base.OnDisposing();
            }
        }
    }
}
