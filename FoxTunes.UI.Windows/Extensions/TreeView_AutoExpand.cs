using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace FoxTunes
{
    public static partial class TreeViewExtensions
    {
        private static readonly ILibraryHierarchyBrowser LibraryHierarchyBrowser = ComponentRegistry.Instance.GetComponent<ILibraryHierarchyBrowser>();

        private static readonly ConditionalWeakTable<TreeView, AutoExpandBehaviour> AutoExpandBehaviours = new ConditionalWeakTable<TreeView, AutoExpandBehaviour>();

        public static readonly DependencyProperty AutoExpandProperty = DependencyProperty.RegisterAttached(
            "AutoExpand",
            typeof(bool),
            typeof(TreeViewExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnAutoExpandPropertyChanged))
        );

        public static bool GetAutoExpand(TreeView source)
        {
            return (bool)source.GetValue(AutoExpandProperty);
        }

        public static void SetAutoExpand(TreeView source, bool value)
        {
            source.SetValue(AutoExpandProperty, value);
        }

        private static void OnAutoExpandPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var treeView = sender as TreeView;
            if (treeView == null)
            {
                return;
            }
            if (GetAutoExpand(treeView))
            {
                var behaviour = default(AutoExpandBehaviour);
                if (!AutoExpandBehaviours.TryGetValue(treeView, out behaviour))
                {
                    AutoExpandBehaviours.Add(treeView, new AutoExpandBehaviour(treeView));
                }
            }
            else
            {
                AutoExpandBehaviours.Remove(treeView);
            }
        }

        private class AutoExpandBehaviour : UIBehaviour<TreeView>
        {
            const int EXPAND_ALL_LIMIT = 5;

            public AutoExpandBehaviour(TreeView treeView) : base(treeView)
            {
                this.TreeView = treeView;
                BindingHelper.AddHandler(
                    this.TreeView,
                    ItemsControl.ItemsSourceProperty,
                    typeof(ListView),
                    this.OnItemsSourceChanged
                );
            }

            public TreeView TreeView { get; private set; }

            public INotifyCollectionChanged ItemsSource { get; private set; }

            protected virtual void OnItemsSourceChanged(object sender, EventArgs e)
            {
                if (this.ItemsSource != null)
                {
                    this.ItemsSource.CollectionChanged -= this.OnCollectionChanged;
                    this.ItemsSource = null;
                }
                if (this.TreeView.ItemsSource is INotifyCollectionChanged notifyCollectionChanged)
                {
                    this.ItemsSource = notifyCollectionChanged;
                    this.ItemsSource.CollectionChanged += this.OnCollectionChanged;
                }
                this.ExpandAllIfRequired();
            }

            protected virtual void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
            {
                this.ExpandAllIfRequired();
            }

            protected virtual void ExpandAllIfRequired()
            {
                if (string.IsNullOrEmpty(LibraryHierarchyBrowser.Filter))
                {
                    return;
                }
                var view = CollectionViewSource.GetDefaultView(this.TreeView.ItemsSource) as CollectionView;
                if (view == null || view.Count > EXPAND_ALL_LIMIT)
                {
                    return;
                }
                var stack = new Stack<LibraryHierarchyNode>(view.OfType<LibraryHierarchyNode>());
                this.Dispatch(() => this.ExpandAll(stack));
            }

            protected virtual async Task ExpandAll(Stack<LibraryHierarchyNode> stack)
            {
                while (stack.Count > 0)
                {
                    var node = stack.Pop();
                    if (!node.IsExpanded)
                    {
                        await Windows.Invoke(() => node.IsExpanded = true).ConfigureAwait(false);
                    }
                    foreach (var child in node.Children)
                    {
                        if (child.IsLeaf)
                        {
                            continue;
                        }
                        stack.Push(child);
                    }
                }
            }

            protected override void OnDisposing()
            {
                if (this.TreeView != null)
                {
                    BindingHelper.RemoveHandler(
                        this.TreeView,
                        ItemsControl.ItemsSourceProperty,
                        typeof(ListView),
                        this.OnItemsSourceChanged
                    );
                }
                if (this.ItemsSource != null)
                {
                    this.ItemsSource.CollectionChanged -= this.OnCollectionChanged;
                }
                base.OnDisposing();
            }
        }
    }
}
