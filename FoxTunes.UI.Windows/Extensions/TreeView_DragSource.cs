using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class TreeViewExtensions
    {
        private static readonly ConditionalWeakTable<TreeView, DragSourceBehaviour> DragSourceBehaviours = new ConditionalWeakTable<TreeView, DragSourceBehaviour>();

        public static readonly DependencyProperty DragSourceProperty = DependencyProperty.RegisterAttached(
            "DragSource",
            typeof(bool),
            typeof(TreeViewExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnDragSourcePropertyChanged))
        );

        public static bool GetDragSource(TreeView source)
        {
            return (bool)source.GetValue(DragSourceProperty);
        }

        public static void SetDragSource(TreeView source, bool value)
        {
            source.SetValue(DragSourceProperty, value);
        }

        private static void OnDragSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var treeView = sender as TreeView;
            if (treeView == null)
            {
                return;
            }
            if (GetDragSource(treeView))
            {
                var behaviour = default(DragSourceBehaviour);
                if (!DragSourceBehaviours.TryGetValue(treeView, out behaviour))
                {
                    DragSourceBehaviours.Add(treeView, new DragSourceBehaviour(treeView));
                }
            }
            else
            {
                DragSourceBehaviours.Remove(treeView);
            }
        }

        public static readonly RoutedEvent DragSourceInitializedEvent = EventManager.RegisterRoutedEvent(
            "DragSourceInitialized",
            RoutingStrategy.Bubble,
            typeof(DragSourceInitializedEventHandler),
            typeof(TreeViewExtensions)
        );

        public static void AddDragSourceInitializedHandler(DependencyObject source, DragSourceInitializedEventHandler handler)
        {
            var element = source as UIElement;
            if (element != null)
            {
                element.AddHandler(DragSourceInitializedEvent, handler);
            }
        }

        public static void RemoveDragSourceInitializedHandler(DependencyObject source, DragSourceInitializedEventHandler handler)
        {
            var element = source as UIElement;
            if (element != null)
            {
                element.RemoveHandler(DragSourceInitializedEvent, handler);
            }
        }

        public delegate void DragSourceInitializedEventHandler(object sender, DragSourceInitializedEventArgs e);

        public class DragSourceInitializedEventArgs : RoutedEventArgs
        {
            public DragSourceInitializedEventArgs(object data)
            {
                this.Data = data;
            }

            public DragSourceInitializedEventArgs(RoutedEvent routedEvent, object data)
                : base(routedEvent)
            {
                this.Data = data;
            }

            public DragSourceInitializedEventArgs(RoutedEvent routedEvent, object source, object data)
                : base(routedEvent, source)
            {
                this.Data = data;
            }

            public object Data { get; private set; }
        }

        private class DragSourceBehaviour : UIBehaviour<TreeView>
        {
            public DragSourceBehaviour(TreeView treeView) : base(treeView)
            {
                this.TreeView = treeView;
                this.TreeView.PreviewMouseDown += this.OnMouseDown;
                this.TreeView.PreviewMouseUp += this.OnMouseUp;
                this.TreeView.MouseMove += this.OnMouseMove;
                this.TreeView.PreviewTouchDown += this.OnTouchDown;
                this.TreeView.PreviewTouchUp += this.OnTouchUp;
                this.TreeView.TouchMove += this.OnTouchMove;
            }

            public Point DragStartPosition { get; private set; }

            public TreeView TreeView { get; private set; }

            protected virtual bool ShouldInitializeDrag(Point position)
            {
                if (this.DragStartPosition.Equals(default(Point)))
                {
                    return false;
                }
                if (Math.Abs(position.X - this.DragStartPosition.X) > (SystemParameters.MinimumHorizontalDragDistance * 2))
                {
                    return true;
                }
                if (Math.Abs(position.Y - this.DragStartPosition.Y) > (SystemParameters.MinimumVerticalDragDistance * 2))
                {
                    return true;
                }
                return false;
            }

            protected virtual void OnMouseDown(object sender, MouseButtonEventArgs e)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    return;
                }
                this.DragStartPosition = e.GetPosition(this.TreeView);
            }

            protected virtual void OnTouchDown(object sender, TouchEventArgs e)
            {
                this.DragStartPosition = e.GetTouchPoint(this.TreeView).Position;
            }

            protected virtual void OnMouseUp(object sender, MouseButtonEventArgs e)
            {
                this.DragStartPosition = default(Point);
            }

            protected virtual void OnTouchUp(object sender, TouchEventArgs e)
            {
                this.DragStartPosition = default(Point);
            }

            protected virtual void OnMouseMove(object sender, MouseEventArgs e)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    return;
                }
                var dependencyObject = e.OriginalSource as DependencyObject;
                if (dependencyObject != null && dependencyObject.FindAncestor<TreeViewItem>() != null)
                {
                    this.TryInitializeDrag(e.GetPosition(this.TreeView));
                }
            }

            protected virtual void OnTouchMove(object sender, TouchEventArgs e)
            {
                var position = e.GetTouchPoint(this.TreeView).Position;
                var result = VisualTreeHelper.HitTest(this.TreeView, position);
                if (result != null && result.VisualHit is DependencyObject dependencyObject && dependencyObject.FindAncestor<TreeViewItem>() != null)
                {
                    this.TryInitializeDrag(position);
                }
            }

            protected virtual bool TryInitializeDrag(Point position)
            {
                var selectedItem = GetSelectedItem(this.TreeView);
                if (selectedItem == null)
                {
                    return false;
                }
                if (this.ShouldInitializeDrag(position))
                {
                    this.DragStartPosition = default(Point);
                    this.TreeView.RaiseEvent(new DragSourceInitializedEventArgs(DragSourceInitializedEvent, selectedItem));
                    return true;
                }
                return false;
            }

            protected override void OnDisposing()
            {
                if (this.TreeView != null)
                {
                    this.TreeView.PreviewMouseDown -= this.OnMouseDown;
                    this.TreeView.PreviewMouseUp -= this.OnMouseUp;
                    this.TreeView.MouseMove -= this.OnMouseMove;
                    this.TreeView.PreviewTouchDown -= this.OnTouchDown;
                    this.TreeView.PreviewTouchUp -= this.OnTouchUp;
                    this.TreeView.TouchMove -= this.OnTouchMove;
                }
                base.OnDisposing();
            }
        }
    }
}
