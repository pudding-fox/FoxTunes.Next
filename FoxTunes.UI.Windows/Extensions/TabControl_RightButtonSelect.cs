using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FoxTunes
{
    public static partial class TabControlExtensions
    {
        private static readonly ConditionalWeakTable<global::System.Windows.Controls.TabControl, RightButtonSelectBehaviour> RightButtonSelectBehaviours = new ConditionalWeakTable<global::System.Windows.Controls.TabControl, RightButtonSelectBehaviour>();

        public static readonly DependencyProperty RightButtonSelectProperty = DependencyProperty.RegisterAttached(
            "RightButtonSelect",
            typeof(bool),
            typeof(TabControlExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnRightButtonSelectPropertyChanged))
        );

        public static bool GetRightButtonSelect(global::System.Windows.Controls.TabControl source)
        {
            return (bool)source.GetValue(RightButtonSelectProperty);
        }

        public static void SetRightButtonSelect(global::System.Windows.Controls.TabControl source, bool value)
        {
            source.SetValue(RightButtonSelectProperty, value);
        }

        private static void OnRightButtonSelectPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var tabControl = sender as global::System.Windows.Controls.TabControl;
            if (tabControl == null)
            {
                return;
            }
            if (GetRightButtonSelect(tabControl))
            {
                var behaviour = default(RightButtonSelectBehaviour);
                if (!RightButtonSelectBehaviours.TryGetValue(tabControl, out behaviour))
                {
                    RightButtonSelectBehaviours.Add(tabControl, new RightButtonSelectBehaviour(tabControl));
                }
            }
            else
            {
                RightButtonSelectBehaviours.Remove(tabControl);

            }
        }

        private class RightButtonSelectBehaviour : UIBehaviour<global::System.Windows.Controls.TabControl>
        {
            public RightButtonSelectBehaviour(global::System.Windows.Controls.TabControl tabControl) : base(tabControl)
            {
                this.TabControl = tabControl;
                this.TabControl.PreviewMouseRightButtonDown += this.OnPreviewMouseRightButtonDown;
            }

            public global::System.Windows.Controls.TabControl TabControl { get; private set; }

            protected virtual void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
            {
                if (e.OriginalSource is DependencyObject dependencyObject)
                {
                    var item = dependencyObject.FindAncestor<TabItem>();
                    if (item != null)
                    {
                        item.Focus();
                    }
                }
            }

            protected override void OnDisposing()
            {
                if (this.TabControl != null)
                {
                    this.TabControl.PreviewMouseRightButtonDown -= this.OnPreviewMouseRightButtonDown;
                }
                base.OnDisposing();
            }
        }
    }
}
