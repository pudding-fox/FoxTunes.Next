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
        private static readonly ConditionalWeakTable<TreeView, EnsureSelectedItemVisibleBehaviour> EnsureSelectedItemVisibleBehaviours = new ConditionalWeakTable<TreeView, EnsureSelectedItemVisibleBehaviour>();

        public static readonly DependencyProperty EnsureSelectedItemVisibleProperty = DependencyProperty.RegisterAttached(
            "EnsureSelectedItemVisible",
            typeof(bool),
            typeof(TreeViewExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnEnsureSelectedItemVisiblePropertyChanged))
        );

        public static bool GetEnsureSelectedItemVisible(TreeView source)
        {
            return (bool)source.GetValue(EnsureSelectedItemVisibleProperty);
        }

        public static void SetEnsureSelectedItemVisible(TreeView source, bool value)
        {
            source.SetValue(EnsureSelectedItemVisibleProperty, value);
        }

        private static void OnEnsureSelectedItemVisiblePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var treeView = sender as TreeView;
            if (treeView == null)
            {
                return;
            }
            if (GetEnsureSelectedItemVisible(treeView))
            {
                var behaviour = default(EnsureSelectedItemVisibleBehaviour);
                if (!EnsureSelectedItemVisibleBehaviours.TryGetValue(treeView, out behaviour))
                {
                    EnsureSelectedItemVisibleBehaviours.Add(treeView, new EnsureSelectedItemVisibleBehaviour(treeView));
                }
            }
            else
            {
                var behaviour = default(EnsureSelectedItemVisibleBehaviour);
                if (EnsureSelectedItemVisibleBehaviours.TryGetValue(treeView, out behaviour))
                {
                    EnsureSelectedItemVisibleBehaviours.Remove(treeView);
                    behaviour.Dispose();
                }
            }
        }

        private class EnsureSelectedItemVisibleBehaviour : UIBehaviour<TreeView>
        {
            public EnsureSelectedItemVisibleBehaviour(TreeView treeView) : base(treeView)
            {
                this.TreeView = treeView;
                BindingHelper.AddHandler(this.TreeView, TreeView.ItemsSourceProperty, typeof(TreeView), this.OnItemsSourceChanged);
            }

            public TreeView TreeView { get; private set; }

            protected virtual bool EnsureVisible(object value)
            {
                if (value is IHierarchical hierarchical)
                {
                    return this.EnsureVisible(hierarchical);
                }
                return false;
            }

            protected virtual bool EnsureVisible(IHierarchical value)
            {
                var stack = new Stack<IHierarchical>();
                do
                {
                    if (value == null)
                    {
                        break;
                    }
                    stack.Push(value);
                    value = value.Parent;
                } while (true);
                if (stack.Count == 0)
                {
                    return false;
                }
                var item = this.EnsureVisible(stack);
                if (item == null)
                {
                    return false;
                }
                return true;
            }

            protected virtual TreeViewItem EnsureVisible(Stack<IHierarchical> stack)
            {
                var offset = default(int);
                var items = default(ItemsControl);
                var item = default(TreeViewItem);
                var scrollViewer = this.TreeView.FindChild<ScrollViewer>();
                do
                {
                    var value = stack.Pop();
                    if (value == null)
                    {
                        break;
                    }
                    if (item != null)
                    {
                        if (!item.IsExpanded)
                        {
                            item.IsExpanded = true;
                        }
                        items = item;
                    }
                    else
                    {
                        items = this.TreeView;
                    }
                    item = this.EnsureVisible(scrollViewer, items, value, ref offset);
                    if (item == null)
                    {
                        break;
                    }
                } while (stack.Count > 0);
                return item;
            }

            protected virtual TreeViewItem EnsureVisible(ScrollViewer scrollViewer, ItemsControl items, object value, ref int offset)
            {
                var index = items.Items.IndexOf(value);
                if (index < 0)
                {
                    return null;
                }
                var item = items.ItemContainerGenerator.ContainerFromItem(value) as TreeViewItem;
                if (item != null)
                {
                    item.BringIntoView();
                    offset += index;
                }
                else if (scrollViewer != null)
                {
                    if (scrollViewer.ScrollToItemOffset<TreeViewItem>(offset + index, this.OnItemLoaded))
                    {
                        items.UpdateLayout();
                        item = items.ItemContainerGenerator.ContainerFromItem(value) as TreeViewItem;
                        offset += index;
                    }
                }
                return item;
            }

            protected virtual void OnItemLoaded(object sender, RoutedEventArgs e)
            {
                var value = GetSelectedItem(this.TreeView);
                if (value != null)
                {
                    this.EnsureVisible(value);
                }
            }

            protected virtual void OnItemsSourceChanged(object sender, EventArgs e)
            {
                var value = GetSelectedItem(this.TreeView);
                if (value is IHierarchical)
                {
                    this.TreeView.ItemContainerGenerator.StatusChanged += this.OnStatusChanged;
                }
                else
                {
                    //Can only restore selection for IHierarchical data.
                }
            }

            protected virtual void OnStatusChanged(object sender, EventArgs e)
            {
                if (this.TreeView.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
                {
                    return;
                }
                this.TreeView.ItemContainerGenerator.StatusChanged -= this.OnStatusChanged;
                var value = GetSelectedItem(this.TreeView);
                if (value != null)
                {
                    this.EnsureVisible(value);
                }
            }

            protected override void OnDisposing()
            {
                if (this.TreeView != null)
                {
                    BindingHelper.RemoveHandler(this.TreeView, TreeView.ItemsSourceProperty, typeof(TreeView), this.OnItemsSourceChanged);
                    this.TreeView.ItemContainerGenerator.StatusChanged -= this.OnStatusChanged;
                }
                base.OnDisposing();
            }
        }
    }
}
