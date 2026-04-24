using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class WindowExtensions
    {
        private static readonly ConditionalWeakTable<Window, DragMoveBehaviour> DragMoveBehaviours = new ConditionalWeakTable<Window, DragMoveBehaviour>();

        public static readonly DependencyProperty DragMoveProperty = DependencyProperty.RegisterAttached(
            "DragMove",
            typeof(bool),
            typeof(WindowExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnDragMovePropertyChanged))
        );

        public static bool GetDragMove(Window source)
        {
            return (bool)source.GetValue(DragMoveProperty);
        }

        public static void SetDragMove(Window source, bool value)
        {
            source.SetValue(DragMoveProperty, value);
        }

        private static void OnDragMovePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var window = sender as Window;
            if (window == null)
            {
                return;
            }
            if (GetDragMove(window))
            {
                var behaviour = default(DragMoveBehaviour);
                if (!DragMoveBehaviours.TryGetValue(window, out behaviour))
                {
                    DragMoveBehaviours.Add(window, new DragMoveBehaviour(window));
                }
            }
            else
            {
                DragMoveBehaviours.Remove(window);
            }
        }

        private class DragMoveBehaviour : UIBehaviour<Window>
        {
            public DragMoveBehaviour(Window window) : base(window)
            {
                this.Window = window;
                this.Window.MouseDown += this.OnMouseDown;
                this.Window.MouseUp += this.OnMouseUp;
                this.Window.MouseMove += this.OnMouseMove;
                this.Window.TouchDown += this.OnTouchDown;
                this.Window.TouchUp += this.OnTouchUp;
                this.Window.TouchMove += this.OnTouchMove;
            }

            public Point DragStartPosition { get; private set; }

            public Window Window { get; private set; }

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
                this.DragStartPosition = e.GetPosition(this.Window);
            }

            protected virtual void OnTouchDown(object sender, TouchEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    return;
                }
                this.DragStartPosition = e.GetTouchPoint(this.Window).Position;
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
                this.TryInitializeDrag(e.GetPosition(this.Window));
            }

            protected virtual void OnTouchMove(object sender, TouchEventArgs e)
            {
                this.TryInitializeDrag(e.GetTouchPoint(this.Window).Position);
            }

            protected virtual bool TryInitializeDrag(Point position)
            {
                if (!this.ShouldInitializeDrag(position))
                {
                    return false;
                }
                this.Window.DragMove();
                return true;
            }

            protected override void OnDisposing()
            {
                if (this.Window != null)
                {
                    this.Window.MouseDown -= this.OnMouseDown;
                    this.Window.MouseUp -= this.OnMouseUp;
                    this.Window.MouseMove -= this.OnMouseMove;
                    this.Window.TouchDown -= this.OnTouchDown;
                    this.Window.TouchUp -= this.OnTouchUp;
                    this.Window.TouchMove -= this.OnTouchMove;
                }
                base.OnDisposing();
            }
        }
    }
}
