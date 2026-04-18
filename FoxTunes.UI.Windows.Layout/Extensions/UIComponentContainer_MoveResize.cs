using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FoxTunes
{
    public static partial class UIComponentContainerExtensions
    {
        private static readonly ConditionalWeakTable<UIComponentContainer, MoveResizeBehaviour> MoveResizeBehaviours = new ConditionalWeakTable<UIComponentContainer, MoveResizeBehaviour>();

        public static readonly DependencyProperty MoveResizeProperty = DependencyProperty.RegisterAttached(
                "MoveResize",
                typeof(bool),
                typeof(UIComponentContainerExtensions),
                new PropertyMetadata(false, OnMoveResizeChanged)
        );

        public static bool GetMoveResize(UIComponentContainer source)
        {
            return (bool)source.GetValue(MoveResizeProperty);
        }

        public static void SetMoveResize(UIComponentContainer source, bool value)
        {
            source.SetValue(MoveResizeProperty, value);
        }

        private static void OnMoveResizeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var container = sender as UIComponentContainer;
            if (container == null)
            {
                return;
            }
            if (GetMoveResize(container))
            {
                MoveResizeBehaviours.Add(container, new MoveResizeBehaviour(container));
            }
            else
            {
                var behaviour = default(MoveResizeBehaviour);
                if (MoveResizeBehaviours.TryRemove(container, out behaviour))
                {
                    behaviour.Dispose();
                }
            }
        }

        private class MoveResizeBehaviour : UIBehaviour<UIComponentContainer>
        {
            public const double MARGIN = 5;

            public const double MIN = 40;

            public MoveResizeBehaviour(UIComponentContainer container) : base(container)
            {
                this.Container = container;
                this.Container.PreviewMouseLeftButtonDown += this.OnPreviewMouseLeftButtonDown;
                this.Container.PreviewMouseMove += this.OnPreviewMouseMove;
                this.Container.PreviewMouseLeftButtonUp += this.OnPreviewMouseLeftButtonUp;
                this.Container.MouseLeave += this.OnMouseLeave;
            }

            public UIComponentContainer Container { get; private set; }

            public MoveResizeBehaviourState State { get; private set; }

            protected virtual void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            {
                var canvas = this.Container.FindAncestor<Canvas>();
                if (canvas == null)
                {
                    return;
                }
                var position = e.GetPosition(this.Container);
                var parentPosition = e.GetPosition(canvas);
                var mode = HitTest(this.Container, position);
                this.State = new MoveResizeBehaviourState();
                this.State.Mode = (mode == Mode.None) ? Mode.Move : mode;
                this.State.Position = parentPosition;
                this.State.Left = GetLeft(this.Container);
                this.State.Top = GetTop(this.Container);
                this.State.Width = this.Container.ActualWidth;
                this.State.Height = this.Container.ActualHeight;
                this.State.Offset = new Point(
                    parentPosition.X - this.State.Left,
                    parentPosition.Y - this.State.Top
                );
                this.Container.CaptureMouse();
                e.Handled = true;
            }

            protected virtual void OnPreviewMouseMove(object sender, MouseEventArgs e)
            {
                var canvas = this.Container.FindAncestor<Canvas>();
                if (canvas == null)
                {
                    return;
                }
                var position = e.GetPosition(this.Container);
                if (this.State == null)
                {
                    this.Container.Cursor = GetCursor(HitTest(this.Container, position));
                    return;
                }
                var parentPosition = e.GetPosition(canvas);
                var vector = parentPosition - this.State.Position;
                switch (this.State.Mode)
                {
                    case Mode.Move:
                        Canvas.SetLeft(this.Container, parentPosition.X - this.State.Offset.X);
                        Canvas.SetTop(this.Container, parentPosition.Y - this.State.Offset.Y);
                        break;
                    case Mode.Right:
                        this.Container.Width = Math.Max(MIN, this.State.Width + vector.X);
                        break;
                    case Mode.Bottom:
                        this.Container.Height = Math.Max(MIN, this.State.Height + vector.Y);
                        break;
                    case Mode.BottomRight:
                        this.Container.Width = Math.Max(MIN, this.State.Width + vector.X);
                        this.Container.Height = Math.Max(MIN, this.State.Height + vector.Y);
                        break;
                    case Mode.Left:
                        this.Container.Width = Math.Max(MIN, this.State.Width - vector.X);
                        Canvas.SetLeft(this.Container, this.State.Left + vector.X);
                        break;
                    case Mode.Top:
                        this.Container.Height = Math.Max(40, this.State.Height - vector.Y);
                        Canvas.SetTop(this.Container, this.State.Top + vector.Y);
                        break;
                    case Mode.TopLeft:
                        this.Container.Width = Math.Max(MIN, this.State.Width - vector.X);
                        this.Container.Height = Math.Max(MIN, this.State.Height - vector.Y);
                        Canvas.SetLeft(this.Container, this.State.Left + vector.X);
                        Canvas.SetTop(this.Container, this.State.Top + vector.Y);
                        break;
                    case Mode.TopRight:
                        this.Container.Width = Math.Max(MIN, this.State.Width + vector.X);
                        this.Container.Height = Math.Max(MIN, this.State.Height - vector.Y);
                        Canvas.SetTop(this.Container, this.State.Top + vector.Y);
                        break;
                    case Mode.BottomLeft:
                        this.Container.Width = Math.Max(40, this.State.Width - vector.X);
                        this.Container.Height = Math.Max(40, this.State.Height + vector.Y);
                        Canvas.SetLeft(this.Container, this.State.Left + vector.X);
                        break;
                }
            }

            protected virtual void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            {
                if (this.State != null)
                {
                    this.Container.ReleaseMouseCapture();
                    this.State = null;
                }
            }

            protected virtual void OnMouseLeave(object sender, MouseEventArgs e)
            {
                if (this.State == null)
                {
                    this.Container.Cursor = Cursors.Arrow;
                }
            }

            private static Mode HitTest(FrameworkElement element, Point point)
            {
                var width = element.ActualWidth;
                var height = element.ActualHeight;
                var left = point.X <= MARGIN;
                var right = point.X >= width - MARGIN;
                var top = point.Y <= MARGIN;
                var bottom = point.Y >= height - MARGIN;
                if (top && left)
                {
                    return Mode.TopLeft;
                }
                if (top && right)
                {
                    return Mode.TopRight;
                }
                if (bottom && left)
                {
                    return Mode.BottomLeft;
                }
                if (bottom && right)
                {
                    return Mode.BottomRight;
                }
                if (top)
                {
                    return Mode.Top;
                }
                if (bottom)
                {
                    return Mode.Bottom;
                }
                if (left)
                {
                    return Mode.Left;
                }
                if (right)
                {
                    return Mode.Right;
                }
                return Mode.None;
            }

            private static Cursor GetCursor(Mode mode)
            {
                if (mode == Mode.Left || mode == Mode.Right)
                {
                    return Cursors.SizeWE;
                }
                if (mode == Mode.Top || mode == Mode.Bottom)
                {
                    return Cursors.SizeNS;
                }
                if (mode == Mode.TopLeft || mode == Mode.BottomRight)
                {
                    return Cursors.SizeNWSE;
                }
                if (mode == Mode.TopRight || mode == Mode.BottomLeft)
                {
                    return Cursors.SizeNESW;
                }
                return Cursors.Arrow;
            }

            private static double GetLeft(UIElement element)
            {
                var left = Canvas.GetLeft(element);
                return double.IsNaN(left) ? 0 : left;
            }

            private static double GetTop(UIElement element)
            {
                var top = Canvas.GetTop(element);
                return double.IsNaN(top) ? 0 : top;
            }

            protected override void OnDisposing()
            {
                if (this.Container != null)
                {
                    this.Container.PreviewMouseLeftButtonDown -= this.OnPreviewMouseLeftButtonDown;
                    this.Container.PreviewMouseMove -= this.OnPreviewMouseMove;
                    this.Container.PreviewMouseLeftButtonUp -= this.OnPreviewMouseLeftButtonUp;
                    this.Container.MouseLeave -= this.OnMouseLeave;
                }
                base.OnDisposing();
            }

            public enum Mode
            {
                None,
                Move,
                Left,
                Right,
                Top,
                Bottom,
                TopLeft,
                TopRight,
                BottomLeft,
                BottomRight
            }

            public class MoveResizeBehaviourState
            {
                public Mode Mode { get; set; }

                public double Left { get; set; }

                public double Top { get; set; }

                public double Width { get; set; }

                public double Height { get; set; }

                public Point Position { get; set; }

                public Point Offset { get; set; }
            }
        }
    }
}