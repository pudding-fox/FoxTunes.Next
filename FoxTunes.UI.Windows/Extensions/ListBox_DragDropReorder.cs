using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class ListBoxExtensions
    {
        private static readonly ConditionalWeakTable<ListBox, DragDropReorderBehaviour> DragDropReorderBehaviours = new ConditionalWeakTable<ListBox, DragDropReorderBehaviour>();

        public static readonly DependencyProperty DragDropReorderProperty = DependencyProperty.RegisterAttached(
            "DragDropReorder",
            typeof(bool),
            typeof(ListBoxExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnDragDropReorderChanged))
        );

        public static bool GetDragDropReorder(ListBox source)
        {
            return (bool)source.GetValue(DragDropReorderProperty);
        }

        public static void SetDragDropReorder(ListBox source, bool value)
        {
            source.SetValue(DragDropReorderProperty, value);
        }

        private static void OnDragDropReorderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            if (listBox == null)
            {
                return;
            }
            if (GetDragDropReorder(listBox))
            {
                var behaviour = default(DragDropReorderBehaviour);
                if (!DragDropReorderBehaviours.TryGetValue(listBox, out behaviour))
                {
                    DragDropReorderBehaviours.Add(listBox, new DragDropReorderBehaviour(listBox));
                }
            }
            else
            {
                DragDropReorderBehaviours.Remove(listBox);
            }
        }

        public static readonly DependencyProperty DragDropReorderCommandProperty = DependencyProperty.RegisterAttached(
            "DragDropReorderCommand",
            typeof(ICommand),
            typeof(ListBoxExtensions)
        );

        public static ICommand GetDragDropReorderCommand(ListBox source)
        {
            return (ICommand)source.GetValue(DragDropReorderCommandProperty);
        }

        public static void SetDragDropReorderCommand(ListBox source, ICommand value)
        {
            source.SetValue(DragDropReorderCommandProperty, value);
        }

        private class DragDropReorderBehaviour : UIBehaviour<ListBox>
        {
            public DragDropReorderBehaviour(ListBox listBox) : base(listBox)
            {
                this.ListBox = listBox;
                this.ListBox.AllowDrop = true;
                this.ListBox.PreviewMouseDown += this.OnMouseDown;
                this.ListBox.PreviewMouseUp += this.OnMouseUp;
                this.ListBox.MouseMove += this.OnMouseMove;
                this.ListBox.DragOver += this.OnDragOver;
                this.ListBox.GiveFeedback += this.OnGiveFeedback;
            }

            public Point DragStartPosition { get; private set; }

            public object DragData { get; private set; }

            public bool DragInitialized { get; private set; }

            public ListBox ListBox { get; private set; }

            protected virtual bool ShouldInitializeDrag(object source, Point position)
            {
                if (this.DragStartPosition.Equals(default(Point)) || source is Thumb)
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

            protected virtual void DoDragDrop()
            {
                this.DragInitialized = true;
                try
                {
                    DragDrop.DoDragDrop(
                        this.ListBox,
                        this.DragData,
                        DragDropEffects.Copy
                    );
                }
                finally
                {
                    this.DragInitialized = false;
                }
            }

            protected virtual void OnMouseDown(object sender, MouseButtonEventArgs e)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    return;
                }
                this.DragStartPosition = e.GetPosition(this.ListBox);
                this.DragData = this.GetDataContext(e);
            }

            protected virtual void OnMouseUp(object sender, MouseButtonEventArgs e)
            {
                this.DragStartPosition = default(Point);
                this.DragData = null;
            }

            protected virtual void OnMouseMove(object sender, MouseEventArgs e)
            {
                if (e.LeftButton != MouseButtonState.Pressed || this.DragInitialized || this.ListBox.SelectedValue == null)
                {
                    return;
                }
                var position = e.GetPosition(this.ListBox);
                if (this.ShouldInitializeDrag(e.OriginalSource, position))
                {
                    if (!object.ReferenceEquals(this.ListBox.SelectedItem, this.DragData))
                    {
                        this.ListBox.SelectedItem = this.DragData;
                    }
                    this.DragStartPosition = default(Point);
                    this.DoDragDrop();
                }
            }

            protected virtual void OnDragOver(object sender, DragEventArgs e)
            {
                if (!this.DragInitialized)
                {
                    return;
                }
                var data = e.Data.GetData<object>();
                var value = this.GetDataContext(e);
                if (value != null && !object.ReferenceEquals(data, value))
                {
                    var command = GetDragDropReorderCommand(this.ListBox);
                    var parameter = new[] { data, value };
                    if (command != null && command.CanExecute(parameter))
                    {
                        command.Execute(parameter);
                    }
                }
                e.Effects = DragDropEffects.Move;
            }

            protected virtual void OnGiveFeedback(object sender, GiveFeedbackEventArgs e)
            {
                if (!this.DragInitialized)
                {
                    return;
                }
                Mouse.SetCursor(Cursors.Hand);
                e.Handled = true;
            }

            protected virtual object GetDataContext(MouseEventArgs e)
            {
                var position = e.GetPosition(this.ListBox);
                return this.GetDataContext(position);
            }

            protected virtual object GetDataContext(DragEventArgs e)
            {
                var position = e.GetPosition(this.ListBox);
                return this.GetDataContext(position);
            }

            protected virtual object GetDataContext(Point position)
            {
                var result = VisualTreeHelper.HitTest(this.ListBox, position);
                if (result != null && result.VisualHit is FrameworkElement element)
                {
                    return element.DataContext;
                }
                return null;
            }

            protected override void OnDisposing()
            {
                if (this.ListBox != null)
                {
                    this.ListBox.PreviewMouseDown -= this.OnMouseDown;
                    this.ListBox.PreviewMouseUp -= this.OnMouseUp;
                    this.ListBox.MouseMove -= this.OnMouseMove;
                    this.ListBox.DragOver -= this.OnDragOver;
                    this.ListBox.GiveFeedback -= this.OnGiveFeedback;
                }
                base.OnDisposing();
            }
        }
    }
}