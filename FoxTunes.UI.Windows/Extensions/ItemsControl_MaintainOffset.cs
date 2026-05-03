using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace FoxTunes
{
    public static partial class ItemsControlExtensions
    {
        private static readonly ConditionalWeakTable<ItemsControl, MaintainOffsetBehaviour> MaintainOffsetBehaviours = new ConditionalWeakTable<ItemsControl, MaintainOffsetBehaviour>();

        public static readonly DependencyProperty MaintainOffsetProperty = DependencyProperty.RegisterAttached(
            "MaintainOffset",
            typeof(bool),
            typeof(ItemsControlExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnMaintainOffsetPropertyChanged))
        );

        public static bool GetMaintainOffset(ItemsControl source)
        {
            return (bool)source.GetValue(MaintainOffsetProperty);
        }

        public static void SetMaintainOffset(ItemsControl source, bool value)
        {
            source.SetValue(MaintainOffsetProperty, value);
        }

        private static void OnMaintainOffsetPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var itemsControl = sender as ItemsControl;
            if (itemsControl == null)
            {
                return;
            }
            if (GetMaintainOffset(itemsControl))
            {
                var behaviour = default(MaintainOffsetBehaviour);
                if (!MaintainOffsetBehaviours.TryGetValue(itemsControl, out behaviour))
                {
                    MaintainOffsetBehaviours.Add(itemsControl, new MaintainOffsetBehaviour(itemsControl));
                }
            }
            else
            {
                var behaviour = default(MaintainOffsetBehaviour);
                if (MaintainOffsetBehaviours.TryGetValue(itemsControl, out behaviour))
                {
                    MaintainOffsetBehaviours.Remove(itemsControl);
                    behaviour.Dispose();
                }
            }
        }

        private class MaintainOffsetBehaviour : UIBehaviour<ItemsControl>
        {
            const int TIMEOUT = 1000;

            public MaintainOffsetBehaviour(ItemsControl itemsControl) : base(itemsControl)
            {
                this.Debouncer = new Debouncer(TIMEOUT);
                this.ItemsControl = itemsControl;
                this.ItemsControl.ItemContainerGenerator.ItemsChanged += this.OnItemsChanged;
                this.ItemsControl.Loaded += this.OnLoaded;
            }

            public Debouncer Debouncer { get; private set; }

            public ItemsControl ItemsControl { get; private set; }

            public ScrollViewer ScrollViewer { get; private set; }

            public double VerticalOffset { get; private set; }

            protected virtual void OnLoaded(object sender, RoutedEventArgs e)
            {
                this.ScrollViewer = this.ItemsControl.FindChild<ScrollViewer>();
                if (this.ScrollViewer != null)
                {
                    BindingHelper.AddHandler(this.ScrollViewer, ScrollViewer.VerticalOffsetProperty, typeof(ScrollViewer), this.OnVerticalOffsetChanged);
                }
            }

            protected virtual void OnItemsChanged(object sender, ItemsChangedEventArgs e)
            {
                var verticalOffset = this.VerticalOffset;
                this.ItemsControl.Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (this.ScrollViewer != null)
                    {
                        this.ScrollViewer.ScrollToVerticalOffset(verticalOffset);
                    }
                }), DispatcherPriority.Loaded);
            }


            protected virtual void OnVerticalOffsetChanged(object sender, EventArgs e)
            {
                this.Debouncer.Exec(() => this.VerticalOffset = this.ScrollViewer.VerticalOffset);
            }

            protected override void OnDisposing()
            {
                if (this.ItemsControl != null)
                {
                    this.ItemsControl.ItemContainerGenerator.ItemsChanged -= this.OnItemsChanged;
                }
                if (this.ScrollViewer != null)
                {
                    BindingHelper.RemoveHandler(this.ScrollViewer, ScrollViewer.VerticalOffsetProperty, typeof(ScrollViewer), this.OnVerticalOffsetChanged);
                }
                base.OnDisposing();
            }
        }
    }
}
