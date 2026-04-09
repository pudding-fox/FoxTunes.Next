using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FoxTunes
{
    public static partial class TreeViewExtensions
    {
        public static readonly object UnsetValue = new object();

        private static readonly ConditionalWeakTable<TreeView, SelectedItemBehaviour> SelectedItemBehaviours = new ConditionalWeakTable<TreeView, SelectedItemBehaviour>();

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.RegisterAttached(
            "SelectedItem",
            typeof(object),
            typeof(TreeViewExtensions),
            new FrameworkPropertyMetadata(UnsetValue, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnSelectedItemPropertyChanged))
        );

        public static object GetSelectedItem(TreeView source)
        {
            return source.GetValue(SelectedItemProperty);
        }

        public static void SetSelectedItem(TreeView source, object value)
        {
            source.SetValue(SelectedItemProperty, value);
        }

        private static void OnSelectedItemPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var treeView = sender as TreeView;
            if (treeView == null)
            {
                return;
            }
            var behaviour = default(SelectedItemBehaviour);
            if (!SelectedItemBehaviours.TryGetValue(treeView, out behaviour))
            {
                behaviour = new SelectedItemBehaviour(treeView);
                SelectedItemBehaviours.Add(treeView, behaviour);
            }
            behaviour.SelectedItem = e.NewValue;
        }

        private class SelectedItemBehaviour : UIBehaviour<TreeView>
        {
            public SelectedItemBehaviour(TreeView treeView) : base(treeView)
            {
                this.TreeView = treeView;
                this.TreeView.SelectedItemChanged += this.OnSelectedItemChanged;
                this.SelectedItem = GetSelectedItem(this.TreeView);
            }

            public TreeView TreeView { get; private set; }

            public object SelectedItem
            {
                get
                {
                    return this.TreeView.SelectedItem;
                }
                set
                {
                    if (object.Equals(this.SelectedItem, value))
                    {
                        return;
                    }
                    this.WithItem(this.SelectedItem, item => item.IsSelected = false);
                    this.WithItem(value, item =>
                    {
                        item.BringIntoView();
                        item.IsSelected = true;
                    });
                }
            }

            protected virtual void WithItem(object value, Action<TreeViewItem> action)
            {
                if (value == null)
                {
                    return;
                }
                var scrollViewer = this.TreeView.FindChild<ScrollViewer>();
                if (scrollViewer == null)
                {
                    var onStatusChanged = default(EventHandler);
                    onStatusChanged = (sender, e) =>
                    {
                        if (this.TreeView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                        {
                            return;
                        }
                        this.TreeView.ItemContainerGenerator.StatusChanged -= onStatusChanged;
                        this.SelectedItem = value;
                    };
                    this.TreeView.ItemContainerGenerator.StatusChanged += onStatusChanged;
                    return;
                }
                if (value is IHierarchical hierarchical)
                {
                    var path = new Stack<IHierarchical>();
                    path.Push(hierarchical);
                    while (hierarchical.Parent != null)
                    {
                        path.Push(hierarchical.Parent);
                        hierarchical = hierarchical.Parent;
                    }
                    this.WithItem(scrollViewer, this.TreeView, path, 0, action);
                }
            }

            protected virtual void WithItem(ScrollViewer scrollViewer, ItemsControl items, Stack<IHierarchical> path, int offset, Action<TreeViewItem> action)
            {
                var item = path.Peek();
                var container = items.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (container != null)
                {
                    if (path.Count > 1)
                    {
                        container.IsExpanded = true;
                        path.Pop();
                        this.WithItem(scrollViewer, container, path, offset + items.Items.IndexOf(item), action);
                    }
                    else
                    {
                        action(container);
                    }
                }
                else
                {
                    var onStatusChanged = default(EventHandler);
                    onStatusChanged = (sender, e) =>
                    {
                        if (items.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                        {
                            return;
                        }
                        items.ItemContainerGenerator.StatusChanged -= onStatusChanged;
                        this.WithItem(scrollViewer, items, path, offset, action);
                    };
                    items.ItemContainerGenerator.StatusChanged += onStatusChanged;
                    scrollViewer.ScrollToItemOffset<TreeViewItem>(offset);
                }
            }

            protected virtual void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
            {
                SetSelectedItem(this.TreeView, this.TreeView.SelectedItem);
            }

            protected override void OnDisposing()
            {
                if (this.TreeView != null)
                {
                    this.TreeView.SelectedItemChanged -= this.OnSelectedItemChanged;
                }
                base.OnDisposing();
            }
        }
    }
}
