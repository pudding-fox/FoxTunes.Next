using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public partial class ListViewExtensions
    {
        private static readonly ConditionalWeakTable<ListView, CleanUpVirtualizedItemBehaviour> CleanUpVirtualizedItemBehaviours = new ConditionalWeakTable<ListView, CleanUpVirtualizedItemBehaviour>();

        public static readonly DependencyProperty CleanUpVirtualizedItemProperty = DependencyProperty.RegisterAttached(
            "CleanUpVirtualizedItem",
            typeof(bool),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnCleanUpVirtualizedItemPropertyChanged))
        );

        public static bool GetCleanUpVirtualizedItem(ListView source)
        {
            return (bool)source.GetValue(CleanUpVirtualizedItemProperty);
        }

        public static void SetCleanUpVirtualizedItem(ListView source, bool value)
        {
            source.SetValue(CleanUpVirtualizedItemProperty, value);
        }

        private static void OnCleanUpVirtualizedItemPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null)
            {
                return;
            }
            if (GetCleanUpVirtualizedItem(listView))
            {
                var behaviour = default(CleanUpVirtualizedItemBehaviour);
                if (!CleanUpVirtualizedItemBehaviours.TryGetValue(listView, out behaviour))
                {
                    CleanUpVirtualizedItemBehaviours.Add(listView, new CleanUpVirtualizedItemBehaviour(listView));
                }
            }
            else
            {
                var behaviour = default(CleanUpVirtualizedItemBehaviour);
                if (CleanUpVirtualizedItemBehaviours.TryGetValue(listView, out behaviour))
                {
                    CleanUpVirtualizedItemBehaviours.Remove(listView);
                    behaviour.Dispose();
                }
            }
        }

        private class CleanUpVirtualizedItemBehaviour : UIBehaviour<ListView>
        {
            public CleanUpVirtualizedItemBehaviour(ListView listView) : base(listView)
            {
                this.ListView = listView;
                VirtualizingStackPanel.AddCleanUpVirtualizedItemHandler(this.ListView, this.OnCleanUpVirtualizedItem);
            }

            public ListView ListView { get; private set; }

            protected virtual void OnCleanUpVirtualizedItem(object sender, CleanUpVirtualizedItemEventArgs e)
            {
                if (e.UIElement is FrameworkElement element)
                {
                    var dataContext = element.DataContext;
                    if (dataContext != null)
                    {
                        OnCleanup(dataContext);
                    }
                }
            }

            protected override void OnDisposing()
            {
                VirtualizingStackPanel.RemoveCleanUpVirtualizedItemHandler(this.ListView, this.OnCleanUpVirtualizedItem);
                base.OnDisposing();
            }
        }

        public static void OnCleanup(object dataContext)
        {
            if (Cleanup == null)
            {
                return;
            }
            Cleanup(typeof(ListViewExtensions), dataContext);
        }

        public static event EventHandler<object> Cleanup;
    }
}
