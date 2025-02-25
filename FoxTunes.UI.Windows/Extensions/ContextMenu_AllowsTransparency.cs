using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace FoxTunes
{
    public static partial class ContextMenuExtensions
    {
        private static readonly ConditionalWeakTable<ContextMenu, AllowsTransparencyBehaviour> AllowsTransparencyBehaviours = new ConditionalWeakTable<ContextMenu, AllowsTransparencyBehaviour>();

        public static readonly DependencyProperty AllowsTransparencyProperty = DependencyProperty.RegisterAttached(
            "AllowsTransparency",
            typeof(bool),
            typeof(ContextMenuExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnAllowsTransparencyPropertyChanged))
        );

        public static bool GetAllowsTransparency(ContextMenu source)
        {
            return (bool)source.GetValue(AllowsTransparencyProperty);
        }

        public static void SetAllowsTransparency(ContextMenu source, bool value)
        {
            source.SetValue(AllowsTransparencyProperty, value);
        }

        private static void OnAllowsTransparencyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var contextMenu = sender as ContextMenu;
            if (contextMenu == null)
            {
                return;
            }
            if (GetAllowsTransparency(contextMenu))
            {
                var behaviour = default(AllowsTransparencyBehaviour);
                if (!AllowsTransparencyBehaviours.TryGetValue(contextMenu, out behaviour))
                {
                    AllowsTransparencyBehaviours.Add(contextMenu, new AllowsTransparencyBehaviour(contextMenu));
                }
            }
            else
            {
                AllowsTransparencyBehaviours.Remove(contextMenu);
            }
        }

        private class AllowsTransparencyBehaviour : UIBehaviour<ContextMenu>
        {
            public AllowsTransparencyBehaviour(ContextMenu contextMenu) : base(contextMenu)
            {
                this.ContextMenu = contextMenu;
                this.ContextMenu.Loaded += this.OnLoaded;
            }

            public ContextMenu ContextMenu { get; private set; }

            protected virtual void OnLoaded(object sender, RoutedEventArgs e)
            {
                var source = (HwndSource)HwndSource.FromVisual(this.ContextMenu);
                WindowExtensions.EnableBlur(source.Handle);
            }

            protected override void OnDisposing()
            {
                if (this.ContextMenu != null)
                {
                    this.ContextMenu.Loaded -= this.OnLoaded;
                }
                base.OnDisposing();
            }
        }
    }
}
