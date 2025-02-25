using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class ListViewExtensions
    {
        private static readonly ConditionalWeakTable<ListView, DragSourceBehaviour> DragSourceBehaviours = new ConditionalWeakTable<ListView, DragSourceBehaviour>();

        public static readonly DependencyProperty DragSourceProperty = DependencyProperty.RegisterAttached(
            "DragSource",
            typeof(bool),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnDragSourcePropertyChanged))
        );

        public static bool GetDragSource(ListView source)
        {
            return (bool)source.GetValue(DragSourceProperty);
        }

        public static void SetDragSource(ListView source, bool value)
        {
            source.SetValue(DragSourceProperty, value);
        }

        private static void OnDragSourcePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listView = sender as ListView;
            if (listView == null)
            {
                return;
            }
            if (GetDragSource(listView))
            {
                var behaviour = default(DragSourceBehaviour);
                if (!DragSourceBehaviours.TryGetValue(listView, out behaviour))
                {
                    DragSourceBehaviours.Add(listView, new DragSourceBehaviour(listView));
                }
            }
            else
            {
                DragSourceBehaviours.Remove(listView);
            }
        }

        public static readonly DependencyProperty DraggingItemsProperty = DependencyProperty.RegisterAttached(
            "DraggingItems",
            typeof(DraggingItemCollection),
            typeof(ListViewExtensions),
            new FrameworkPropertyMetadata(null, new PropertyChangedCallback(OnDraggingItemsPropertyChanged))
        );

        public static DraggingItemCollection GetDraggingItems(ListView source)
        {
            return (DraggingItemCollection)source.GetValue(DraggingItemsProperty);
        }

        public static void SetDraggingItems(ListView source, DraggingItemCollection value)
        {
            source.SetValue(DraggingItemsProperty, value);
        }

        private static void OnDraggingItemsPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            //Nothing to do.
        }

        public static readonly RoutedEvent DragSourceInitializedEvent = EventManager.RegisterRoutedEvent(
            "DragSourceInitialized",
            RoutingStrategy.Bubble,
            typeof(DragSourceInitializedEventHandler),
            typeof(ListViewExtensions)
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

        private class DragSourceBehaviour : UIBehaviour<ListView>
        {
            public DragSourceBehaviour(ListView listView) : base(listView)
            {
                this.ListView = listView;
                this.ListView.PreviewMouseDown += this.OnMouseDown;
                this.ListView.PreviewMouseUp += this.OnMouseUp;
                this.ListView.MouseMove += this.OnMouseMove;
                this.ListView.PreviewTouchDown += this.OnTouchDown;
                this.ListView.PreviewTouchUp += this.OnTouchUp;
                this.ListView.TouchMove += this.OnTouchMove;
                SetDraggingItems(this.ListView, new DraggingItemCollection(this.ListView));
            }

            public Point DragStartPosition { get; private set; }

            public ListView ListView { get; private set; }

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
                this.DragStartPosition = e.GetPosition(this.ListView);
                this.PrepareDrag(e);
            }

            protected virtual void OnTouchDown(object sender, TouchEventArgs e)
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift) || Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
                {
                    return;
                }
                this.DragStartPosition = e.GetTouchPoint(this.ListView).Position;
                this.PrepareDrag(e);
            }

            protected virtual void PrepareDrag(RoutedEventArgs e)
            {
                if (e.OriginalSource is FrameworkElement frameworkElement)
                {
                    var listViewItem = frameworkElement.FindAncestor<ListViewItem>();
                    if (listViewItem != null && listViewItem.IsSelected)
                    {
                        GetDraggingItems(this.ListView).Update();
                    }
                }
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
                GetDraggingItems(this.ListView).Clear();
            }

            protected virtual void OnMouseMove(object sender, MouseEventArgs e)
            {
                if (e.LeftButton != MouseButtonState.Pressed)
                {
                    return;
                }
                var dependencyObject = e.OriginalSource as DependencyObject;
                if (dependencyObject == null || dependencyObject.FindAncestor<ListViewItem>() == null)
                {
                    return;
                }
                this.TryInitializeDrag(e.GetPosition(this.ListView));
            }

            protected virtual void OnTouchMove(object sender, TouchEventArgs e)
            {
                var position = e.GetTouchPoint(this.ListView).Position;
                var result = VisualTreeHelper.HitTest(this.ListView, position);
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
                var draggingItems = GetDraggingItems(this.ListView);
                if (draggingItems == null || draggingItems.Count == 0)
                {
                    return false;
                }
                this.DragStartPosition = default(Point);
                this.ListView.SelectedItems.Clear();
                this.ListView.RaiseEvent(new DragSourceInitializedEventArgs(DragSourceInitializedEvent, draggingItems));
                foreach (var selectedItem in draggingItems)
                {
                    this.ListView.SelectedItems.Add(selectedItem);
                }
                draggingItems.Clear();
                return true;
            }

            protected override void OnDisposing()
            {
                if (this.ListView != null)
                {
                    this.ListView.PreviewMouseDown -= this.OnMouseDown;
                    this.ListView.PreviewMouseUp -= this.OnMouseUp;
                    this.ListView.MouseMove -= this.OnMouseMove;
                    this.ListView.PreviewTouchDown -= this.OnTouchDown;
                    this.ListView.PreviewTouchUp -= this.OnTouchUp;
                    this.ListView.TouchMove -= this.OnTouchMove;
                    SetDraggingItems(this.ListView, null);
                }
                base.OnDisposing();
            }
        }

        public class DraggingItemCollection : IEnumerable
        {
            private DraggingItemCollection()
            {
                this.Items = new List<object>();
            }

            public DraggingItemCollection(ListView listView) : this()
            {
                this.ListView = listView;
            }

            public IList Items { get; private set; }

            public ListView ListView { get; private set; }

            public void Update()
            {
                var selectedItems = GetSelectedItems(this.ListView);
                if (selectedItems != null)
                {
                    foreach (var selectedItem in selectedItems)
                    {
                        var listViewItem = this.ListView.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListViewItem;
                        if (listViewItem != null)
                        {
                            if (!ListViewItemExtensions.GetIsDragging(listViewItem))
                            {
                                ListViewItemExtensions.SetIsDragging(listViewItem, true);
                            }
                        }
                        this.Items.Add(selectedItem);
                    }
                }
                for (var position = this.Items.Count - 1; position >= 0; position--)
                {
                    var draggingItem = this.Items[position];
                    if (selectedItems == null || !selectedItems.Contains(draggingItem))
                    {
                        var listViewItem = this.ListView.ItemContainerGenerator.ContainerFromItem(draggingItem) as ListViewItem;
                        if (listViewItem != null)
                        {
                            ListViewItemExtensions.SetIsDragging(listViewItem, false);
                        }
                        this.Items.RemoveAt(position);
                    }
                }
            }

            public void Clear()
            {
                foreach (var draggingItem in this.Items)
                {
                    var listViewItem = this.ListView.ItemContainerGenerator.ContainerFromItem(draggingItem) as ListViewItem;
                    if (listViewItem != null)
                    {
                        ListViewItemExtensions.SetIsDragging(listViewItem, false);
                    }
                }
                this.Items.Clear();
            }

            public int Count
            {
                get
                {
                    return this.Items.Count;
                }
            }

            public IEnumerator GetEnumerator()
            {
                return this.Items.GetEnumerator();
            }
        }
    }
}
