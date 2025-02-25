using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class ListBoxExtensions
    {
        private static readonly ConditionalWeakTable<ListBox, DragSourceBehaviour> DragSourceBehaviours = new ConditionalWeakTable<ListBox, DragSourceBehaviour>();

        public static readonly DependencyProperty DragSourceProperty = DependencyProperty.RegisterAttached(
            "DragSource",
            typeof(bool),
            typeof(ListBoxExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnDragSourcePropertyChanged))
        );

        public static bool GetDragSource(ListBox source)
        {
            return (bool)source.GetValue(DragSourceProperty);
        }

        public static void SetDragSource(ListBox source, bool value)
        {
            source.SetValue(DragSourceProperty, value);
        }

        private static void OnDragSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null)
            {
                return;
            }
            if (GetDragSource(listBox))
            {
                var behaviour = default(DragSourceBehaviour);
                if (!DragSourceBehaviours.TryGetValue(listBox, out behaviour))
                {
                    DragSourceBehaviours.Add(listBox, new DragSourceBehaviour(listBox));
                }
            }
            else
            {
                var behaviour = default(DragSourceBehaviour);
                if (DragSourceBehaviours.TryGetValue(listBox, out behaviour))
                {
                    DragSourceBehaviours.Remove(listBox);
                    behaviour.Dispose();
                }
            }
        }

        public static readonly RoutedEvent DragSourceInitializedEvent = EventManager.RegisterRoutedEvent(
            "DragSourceInitialized",
            RoutingStrategy.Bubble,
            typeof(DragSourceInitializedEventHandler),
            typeof(ListBoxExtensions)
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

        private class DragSourceBehaviour : UIBehaviour<ListBox>
        {
            public DragSourceBehaviour(ListBox listBox) : base(listBox)
            {
                this.ListBox = listBox;
                this.ListBox.PreviewMouseDown += this.OnMouseDown;
                this.ListBox.PreviewMouseUp += this.OnMouseUp;
                this.ListBox.MouseMove += this.OnMouseMove;
                this.ListBox.PreviewTouchDown += this.OnTouchDown;
                this.ListBox.PreviewTouchUp += this.OnTouchUp;
                this.ListBox.TouchMove += this.OnTouchMove;
            }

            public Point DragStartPosition { get; private set; }

            public ListBox ListBox { get; private set; }

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
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    return;
                }
                this.DragStartPosition = e.GetPosition(this.ListBox);
                this.PrepareDrag(e);
            }

            protected virtual void OnTouchDown(object sender, TouchEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    return;
                }
                this.DragStartPosition = e.GetTouchPoint(this.ListBox).Position;
                this.PrepareDrag(e);
            }

            protected virtual void PrepareDrag(RoutedEventArgs e)
            {
                //Nothing to do.
            }

            protected virtual void OnMouseUp(object sender, MouseButtonEventArgs e)
            {
                this.EndDrag();
            }

            protected virtual void OnTouchUp(object sender, TouchEventArgs e)
            {
                this.EndDrag();
            }

            protected virtual void EndDrag()
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
                if (dependencyObject == null || dependencyObject.FindAncestor<ListBoxItem>() == null)
                {
                    return;
                }
                this.TryInitializeDrag(e.GetPosition(this.ListBox));
            }

            protected virtual void OnTouchMove(object sender, TouchEventArgs e)
            {
                var position = e.GetTouchPoint(this.ListBox).Position;
                var result = VisualTreeHelper.HitTest(this.ListBox, position);
                if (result != null && result.VisualHit is DependencyObject dependencyObject && dependencyObject.FindAncestor<ListBoxItem>() != null)
                {
                    this.TryInitializeDrag(position);
                }
            }

            protected virtual bool TryInitializeDrag(Point position)
            {
                if (!this.ShouldInitializeDrag(position))
                {
                    return false;
                }
                var selectedItem = this.ListBox.SelectedItem;
                if (selectedItem == null)
                {
                    return false;
                }
                this.DragStartPosition = default(Point);
                this.ListBox.RaiseEvent(new DragSourceInitializedEventArgs(DragSourceInitializedEvent, selectedItem));
                return true;
            }

            protected override void OnDisposing()
            {
                if (this.ListBox != null)
                {
                    this.ListBox.PreviewMouseDown -= this.OnMouseDown;
                    this.ListBox.PreviewMouseUp -= this.OnMouseUp;
                    this.ListBox.MouseMove -= this.OnMouseMove;
                    this.ListBox.PreviewTouchDown -= this.OnTouchDown;
                    this.ListBox.PreviewTouchUp -= this.OnTouchUp;
                    this.ListBox.TouchMove -= this.OnTouchMove;
                }
                base.OnDisposing();
            }
        }
    }
}
