using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

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

        private class SelectedItemBehaviour : EnsureSelectedItemVisibleBehaviour
        {
            public SelectedItemBehaviour(TreeView treeView) : base(treeView)
            {
                this.TreeView.SelectedItemChanged += this.OnSelectedItemChanged;
            }

            public object SelectedItem
            {
                get
                {
                    return this.TreeView.SelectedItem;
                }
                set
                {
                    if (value != null)
                    {
                        this.EnsureVisible(value);
                    }
                }
            }

            protected override TreeViewItem EnsureVisible(Stack<IHierarchical> stack)
            {
                var item = base.EnsureVisible(stack);
                if (item != null && !item.IsSelected)
                {
                    item.IsSelected = true;
                }
                return item;
            }

            protected virtual void OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
            {
                if (e.NewValue == null || object.ReferenceEquals(GetSelectedItem(this.TreeView), e.NewValue))
                {
                    return;
                }
                SetSelectedItem(this.TreeView, e.NewValue);
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
