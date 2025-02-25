using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FoxTunes
{
    public static partial class TreeViewExtensions
    {
        private static readonly ConditionalWeakTable<TreeView, RightButtonSelectBehaviour> RightButtonSelectBehaviours = new ConditionalWeakTable<TreeView, RightButtonSelectBehaviour>();

        public static readonly DependencyProperty RightButtonSelectProperty = DependencyProperty.RegisterAttached(
            "RightButtonSelect",
            typeof(bool),
            typeof(TreeViewExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnRightButtonSelectPropertyChanged))
        );

        public static bool GetRightButtonSelect(TreeView source)
        {
            return (bool)source.GetValue(RightButtonSelectProperty);
        }

        public static void SetRightButtonSelect(TreeView source, bool value)
        {
            source.SetValue(RightButtonSelectProperty, value);
        }

        private static void OnRightButtonSelectPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var treeView = sender as TreeView;
            if (treeView == null)
            {
                return;
            }
            if (GetRightButtonSelect(treeView))
            {
                var behaviour = default(RightButtonSelectBehaviour);
                if (!RightButtonSelectBehaviours.TryGetValue(treeView, out behaviour))
                {
                    RightButtonSelectBehaviours.Add(treeView, new RightButtonSelectBehaviour(treeView));
                }
            }
            else
            {
                RightButtonSelectBehaviours.Remove(treeView);

            }
        }

        private class RightButtonSelectBehaviour : UIBehaviour<TreeView>
        {
            public RightButtonSelectBehaviour(TreeView treeView) : base(treeView)
            {
                this.TreeView = treeView;
                this.TreeView.PreviewMouseRightButtonDown += this.OnPreviewMouseRightButtonDown;
            }

            public TreeView TreeView { get; private set; }

            protected virtual void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
            {
                if (e.OriginalSource is DependencyObject dependencyObject)
                {
                    var item = dependencyObject.FindAncestor<TreeViewItem>();
                    if (item != null)
                    {
                        item.Focus();
                    }
                }
            }

            protected override void OnDisposing()
            {
                if (this.TreeView != null)
                {
                    this.TreeView.PreviewMouseRightButtonDown -= this.OnPreviewMouseRightButtonDown;
                }
                base.OnDisposing();
            }
        }
    }
}
