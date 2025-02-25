using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public static partial class TreeViewExtensions
    {
        private static readonly ConditionalWeakTable<TreeView, ItemContainerStyleBehaviour> ItemContainerStyleBehaviours = new ConditionalWeakTable<TreeView, ItemContainerStyleBehaviour>();

        public static readonly DependencyProperty ItemContainerStyleProperty = DependencyProperty.RegisterAttached(
            "ItemContainerStyle",
            typeof(Style),
            typeof(TreeViewExtensions),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnItemContainerStylePropertyChanged))
        );

        public static Style GetItemContainerStyle(TreeView source)
        {
            return (Style)source.GetValue(ItemContainerStyleProperty);
        }

        public static void SetItemContainerStyle(TreeView source, Style value)
        {
            source.SetValue(ItemContainerStyleProperty, value);
        }

        private static void OnItemContainerStylePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var treeView = sender as TreeView;
            if (treeView == null)
            {
                return;
            }
            if (GetItemContainerStyle(treeView) != null)
            {
                var behaviour = default(ItemContainerStyleBehaviour);
                if (!ItemContainerStyleBehaviours.TryGetValue(treeView, out behaviour))
                {
                    ItemContainerStyleBehaviours.Add(treeView, new ItemContainerStyleBehaviour(treeView));
                }
            }
            else
            {
                ItemContainerStyleBehaviours.Remove(treeView);
            }
        }

        private class ItemContainerStyleBehaviour : DynamicStyleBehaviour<TreeView>
        {
            public ItemContainerStyleBehaviour(TreeView treeView) : base(treeView)
            {
                this.TreeView = treeView;
                this.Apply();
            }

            public TreeView TreeView { get; private set; }

            protected override void Apply()
            {
                this.TreeView.ItemContainerStyle = this.CreateStyle(
                    GetItemContainerStyle(this.TreeView),
                    (Style)this.TreeView.TryFindResource(typeof(TreeViewItem))
                );
            }
        }
    }
}
