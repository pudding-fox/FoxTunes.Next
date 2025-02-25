using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class ListViewExtensions
    {
        private static readonly ConditionalWeakTable<ListView, DropInsertBehaviour> DropInsertBehaviours = new ConditionalWeakTable<ListView, DropInsertBehaviour>();

        public static readonly DependencyProperty DropInsertProperty = DependencyProperty.RegisterAttached(
            "DropInsert",
            typeof(bool),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnDropInsertPropertyChanged))
        );

        public static bool GetDropInsert(ListView source)
        {
            return (bool)source.GetValue(DropInsertProperty);
        }

        public static void SetDropInsert(ListView source, bool value)
        {
            source.SetValue(DropInsertProperty, value);
        }

        private static void OnDropInsertPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null)
            {
                return;
            }
            if (GetDropInsert(listView))
            {
                var behaviour = default(DropInsertBehaviour);
                if (!DropInsertBehaviours.TryGetValue(listView, out behaviour))
                {
                    DropInsertBehaviours.Add(listView, new DropInsertBehaviour(listView));
                }
            }
            else
            {
                DropInsertBehaviours.Remove(listView);
            }
        }

        public static readonly DependencyProperty DropInsertActiveProperty = DependencyProperty.RegisterAttached(
            "DropInsertActive",
            typeof(bool),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static bool GetDropInsertActive(ListView source)
        {
            return (bool)source.GetValue(DropInsertActiveProperty);
        }

        public static void SetDropInsertActive(ListView source, bool value)
        {
            source.SetValue(DropInsertActiveProperty, value);
        }

        public static readonly DependencyProperty DropInsertPenProperty = DependencyProperty.RegisterAttached(
            "DropInsertPen",
            typeof(Pen),
            typeof(ListViewExtensions)
        );

        public static Pen GetDropInsertPen(ListView source)
        {
            return (Pen)source.GetValue(DropInsertPenProperty);
        }

        public static void SetDropInsertPen(ListView source, Pen value)
        {
            source.SetValue(DropInsertPenProperty, value);
        }

        public static readonly DependencyProperty DropInsertItemProperty = DependencyProperty.RegisterAttached(
            "DropInsertItem",
            typeof(ListViewItem),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static ListViewItem GetDropInsertItem(ListView source)
        {
            return (ListViewItem)source.GetValue(DropInsertItemProperty);
        }

        public static void SetDropInsertItem(ListView source, ListViewItem value)
        {
            source.SetValue(DropInsertItemProperty, value);
        }

        public static readonly DependencyProperty DropInsertValueProperty = DependencyProperty.RegisterAttached(
            "DropInsertValue",
            typeof(object),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
        );

        public static object GetDropInsertValue(ListView source)
        {
            return source.GetValue(DropInsertValueProperty);
        }

        public static void SetDropInsertValue(ListView source, object value)
        {
            source.SetValue(DropInsertValueProperty, value);
        }

        private class DropInsertBehaviour : UIBehaviour<ListView>
        {
            public static readonly TimeSpan INTERVAL = TimeSpan.FromSeconds(1);

            public DropInsertBehaviour(ListView listView) : base(listView)
            {
                this.Timer = new global::System.Windows.Threading.DispatcherTimer();
                this.Timer.Interval = INTERVAL;
                this.Timer.Tick += this.OnTick;
                this.ListView = listView;
                this.ListView.DragEnter += this.OnDragEnter;
                this.ListView.DragOver += this.OnDragOver;
                this.ListView.Drop += this.OnDrop;
                this.ListView.DragLeave += this.OnDragLeave;
                this.ListView.QueryContinueDrag += this.OnQueryContinueDrag;
                this.Adorner = new DropInsertAdorner(listView);
            }

            public global::System.Windows.Threading.DispatcherTimer Timer { get; private set; }

            public ListView ListView { get; private set; }

            public DropInsertAdorner Adorner { get; private set; }

            protected virtual void OnTick(object sender, EventArgs e)
            {
                //TODO: Use only WPF frameworks.
                //if (global::System.Windows.Input.Mouse.LeftButton.HasFlag(global::System.Windows.Input.MouseButtonState.Pressed))
                if (global::System.Windows.Forms.Control.MouseButtons.HasFlag(global::System.Windows.Forms.MouseButtons.Left))
                {
                    return;
                }
                this.RemoveAdorner();
            }

            protected virtual void OnDragEnter(object sender, DragEventArgs e)
            {
                this.Add();
            }

            protected virtual void OnDragOver(object sender, DragEventArgs e)
            {
                this.UpdateDropInsertIndex(e);
            }

            protected virtual void OnDrop(object sender, DragEventArgs e)
            {
                this.RemoveAdorner();
            }

            protected virtual void OnDragLeave(object sender, DragEventArgs e)
            {
                if (!this.ListView.IsMouseOver())
                {
                    this.Remove();
                }
            }

            protected virtual void OnQueryContinueDrag(object sender, QueryContinueDragEventArgs e)
            {
                if (e.Action == DragAction.Cancel || e.EscapePressed)
                {
                    this.Remove();
                }
            }

            protected virtual void Add()
            {
                this.AddAdorner();
                SetDropInsertActive(this.ListView, true);
                SetDropInsertItem(this.ListView, null);
                SetDropInsertValue(this.ListView, null);
            }

            protected virtual void AddAdorner()
            {
                if (this.Adorner.Parent == null)
                {
                    var layer = AdornerLayer.GetAdornerLayer(this.ListView);
                    layer.IsHitTestVisible = false;
                    layer.Add(this.Adorner);
                }
                this.Timer.Start();
            }

            protected virtual void Remove()
            {
                this.RemoveAdorner();
                SetDropInsertActive(this.ListView, false);
                SetDropInsertItem(this.ListView, null);
                SetDropInsertValue(this.ListView, null);
            }

            protected virtual void RemoveAdorner()
            {
                if (this.Adorner.Parent != null)
                {
                    var layer = AdornerLayer.GetAdornerLayer(this.ListView);
                    layer.Remove(this.Adorner);
                }
                this.Timer.Stop();
            }

            protected virtual void UpdateDropInsertIndex(DragEventArgs e)
            {
                var listViewItem = this.GetListViewItem(e);
                if (listViewItem != null)
                {
                    SetDropInsertActive(this.ListView, true);
                    SetDropInsertItem(this.ListView, listViewItem);
                    SetDropInsertValue(this.ListView, listViewItem.DataContext);
                }
                else
                {
                    SetDropInsertActive(this.ListView, false);
                    SetDropInsertItem(this.ListView, null);
                    SetDropInsertValue(this.ListView, null);
                }
                this.Adorner.InvalidateVisual();
            }

            protected virtual ListViewItem GetListViewItem(DragEventArgs e)
            {
                //This routine is kind of silly.
                //We should be able to simply hit test the ListView to find the ListViewItem under the cursor.
                //You *can* do that and it works most of the time.
                //The problem is that there are small gaps between the items, this results in the hit test returning the ScrollViewer.
                //In this case it's not easy to determine whether the mouse is between items or below the last item.
                //It would be rare to actually "drop" between items but this issue does result in the adorner flickering as the cursor
                //sweeps across the items (as each missed hit test between items disables it).
                //Instead we just find the item whose mid point is closest to the cursor Y position. X is ignored as items are assumed 
                //full width.
                for (var position = 0; position < this.ListView.Items.Count; position++)
                {
                    var listViewItem = this.ListView.ItemContainerGenerator.ContainerFromIndex(position) as ListViewItem;
                    if (listViewItem == null)
                    {
                        continue;
                    }
                    var point = e.GetPosition(listViewItem);
                    var bounds = VisualTreeHelper.GetDescendantBounds(listViewItem);
                    if (point.Y < bounds.Height / 2)
                    {
                        return listViewItem;
                    }
                }
                return null;
            }

            protected override void OnDisposing()
            {
                if (this.Timer != null)
                {
                    this.Timer.Tick -= this.OnTick;
                }
                if (this.ListView != null)
                {
                    this.ListView.DragEnter -= this.OnDragEnter;
                    this.ListView.DragOver -= this.OnDragOver;
                    this.ListView.Drop -= this.OnDrop;
                    this.ListView.DragLeave -= this.OnDragLeave;
                    this.ListView.QueryContinueDrag -= this.OnQueryContinueDrag;
                }
                base.OnDisposing();
            }
        }

        private class DropInsertAdorner : Adorner
        {
            public DropInsertAdorner(ListView listView) : base(listView)
            {
                this.ListView = listView;
                this.IsHitTestVisible = false;
            }

            public ListView ListView { get; private set; }

            protected override void OnRender(DrawingContext context)
            {
                var item = GetDropInsertItem(this.ListView);
                var listViewItem = GetDropInsertItem(this.ListView);
                if (listViewItem != null && listViewItem.IsDescendantOf(this.ListView))
                {
                    this.RenderInsertionMarker(context, listViewItem);
                }
                base.OnRender(context);
            }

            protected virtual void RenderInsertionMarker(DrawingContext context, ListViewItem listViewItem)
            {
                var scrollViewer = listViewItem.FindAncestor<ScrollViewer>();
                //Translate the top left corner of the item to the position on the list view.
                var point = listViewItem.TransformToAncestor(this.ListView).Transform(new Point());
                var pen = GetDropInsertPen(this.ListView);
                //The horizontal line.
                context.DrawLine(
                    pen,
                    point,
                    new Point(point.X + scrollViewer.ViewportWidth, point.Y)
                );
                //The left vertical nubbin.
                context.DrawLine(
                    pen,
                    new Point(point.X, (point.Y - 5) - pen.Thickness),
                    new Point(point.X, point.Y + 10)
                );
                //The right vertical nubbin.
                context.DrawLine(
                    pen,
                    new Point(point.X + scrollViewer.ViewportWidth, (point.Y - 5) - pen.Thickness),
                    new Point(point.X + scrollViewer.ViewportWidth, point.Y + 10)
                );
            }
        }
    }
}
