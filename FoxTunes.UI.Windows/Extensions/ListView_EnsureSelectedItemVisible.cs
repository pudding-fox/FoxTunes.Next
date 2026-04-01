using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FoxTunes
{
    public static partial class ListViewExtensions
    {
        private static readonly ConditionalWeakTable<ListView, EnsureSelectedItemVisibleBehaviour> EnsureSelectedItemVisibleBehaviours = new ConditionalWeakTable<ListView, EnsureSelectedItemVisibleBehaviour>();

        public static readonly DependencyProperty EnsureSelectedItemVisibleProperty = DependencyProperty.RegisterAttached(
            "EnsureSelectedItemVisible",
            typeof(bool),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnEnsureSelectedItemVisiblePropertyChanged))
        );

        public static bool GetEnsureSelectedItemVisible(ListView source)
        {
            return (bool)source.GetValue(EnsureSelectedItemVisibleProperty);
        }

        public static void SetEnsureSelectedItemVisible(ListView source, bool value)
        {
            source.SetValue(EnsureSelectedItemVisibleProperty, value);
        }

        private static void OnEnsureSelectedItemVisiblePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null)
            {
                return;
            }
            if (GetEnsureSelectedItemVisible(listView))
            {
                var behaviour = default(EnsureSelectedItemVisibleBehaviour);
                if (!EnsureSelectedItemVisibleBehaviours.TryGetValue(listView, out behaviour))
                {
                    EnsureSelectedItemVisibleBehaviours.Add(listView, new EnsureSelectedItemVisibleBehaviour(listView));
                }
            }
            else
            {
                var behaviour = default(EnsureSelectedItemVisibleBehaviour);
                if (EnsureSelectedItemVisibleBehaviours.TryGetValue(listView, out behaviour))
                {
                    EnsureSelectedItemVisibleBehaviours.Remove(listView);
                    behaviour.Dispose();
                }
            }
        }

        private class EnsureSelectedItemVisibleBehaviour : UIBehaviour<ListView>
        {
            public EnsureSelectedItemVisibleBehaviour(ListView listView) : base(listView)
            {
                this.ListView = listView;
                this.ListView.SelectionChanged += this.OnSelectionChanged;
                BindingHelper.AddHandler(
                    this.ListView,
                    global::System.Windows.Controls.ListView.ItemsSourceProperty,
                    typeof(global::System.Windows.Controls.ListView),
                    this.OnItemsSourceChanged
                );
            }

            public ListView ListView { get; private set; }

            protected virtual bool EnsureVisible(object value)
            {
                if (value == null)
                {
                    return false;
                }
                var index = this.ListView.Items.IndexOf(value);
                if (index < 0)
                {
                    return false;
                }
                var item = this.ListView.ItemContainerGenerator.ContainerFromItem(value) as ListViewItem;
                if (item != null)
                {
                    return true;
                }
                else
                {
                    var scrollViewer = this.ListView.FindChild<ScrollViewer>();
                    if (scrollViewer != null)
                    {
                        if (scrollViewer.ScrollToItemOffset<ListViewItem>(index))
                        {
                            this.ListView.UpdateLayout();
                            item = this.ListView.ItemContainerGenerator.ContainerFromItem(value) as ListViewItem;
                            if (item != null)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }

            protected virtual void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
            {
                this.EnsureVisible(this.ListView.SelectedItem);
            }

            protected virtual void OnItemsSourceChanged(object sender, EventArgs e)
            {
                var selectedItems = GetSelectedItems(this.ListView);
                if (selectedItems == null || selectedItems.Count == 0)
                {
                    return;
                }
                if (this.EnsureVisible(selectedItems[0]))
                {
                    return;
                }
                this.ListView.ItemContainerGenerator.StatusChanged += this.OnStatusChanged;
            }

            protected virtual void OnStatusChanged(object sender, EventArgs e)
            {
                if (this.ListView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                {
                    return;
                }
                this.ListView.ItemContainerGenerator.StatusChanged -= this.OnStatusChanged;
                var selectedItems = GetSelectedItems(this.ListView);
                if (selectedItems == null || selectedItems.Count == 0)
                {
                    return;
                }
                this.EnsureVisible(selectedItems[0]);
            }

            protected override void OnDisposing()
            {
                if (this.ListView != null)
                {
                    this.ListView.SelectionChanged -= this.OnSelectionChanged;
                    BindingHelper.RemoveHandler(
                        this.ListView,
                        global::System.Windows.Controls.ListView.ItemsSourceProperty,
                        typeof(global::System.Windows.Controls.ListView),
                        this.OnItemsSourceChanged
                    );
                }
                base.OnDisposing();
            }
        }
    }
}
