#if NET40
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FoxTunes
{
    public class VirtualizingWrapPanel : VirtualizingPanel, IScrollInfo
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public VirtualizingWrapPanel()
        {
            this.ContainerLayouts = new Dictionary<int, Rect>();
            this.PreviousSize = new Size(16, 16);
        }

        public Dictionary<int, Rect> ContainerLayouts { get; private set; }

        public Size PreviousSize { get; private set; }

        public Size Extent { get; private set; }

        public double ExtentHeight
        {
            get
            {
                return this.Extent.Height;
            }
        }

        public double ExtentWidth
        {
            get
            {
                return this.Extent.Width;
            }
        }

        public Size Viewport { get; private set; }

        public double ViewportHeight
        {
            get
            {
                return this.Viewport.Height;
            }
        }

        public double ViewportWidth
        {
            get
            {
                return this.Viewport.Width;
            }
        }

        public Point Offset { get; private set; }

        public double HorizontalOffset
        {
            get
            {
                return this.Offset.X;
            }
        }

        public double VerticalOffset
        {
            get
            {
                return this.Offset.Y;
            }
        }

        public ScrollViewer ScrollOwner { get; set; }

        public bool CanHorizontallyScroll { get; set; }

        public bool CanVerticallyScroll { get; set; }

        protected override void OnItemsChanged(object sender, ItemsChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                case NotifyCollectionChangedAction.Move:
                    this.RemoveInternalChildRange(e.Position.Index, e.ItemUICount);
                    break;
            }
        }

        protected override void BringIndexIntoView(int index)
        {
            var size = this.ContainerSizeForIndex(index);
            var capacity = Math.Floor(this.ActualWidth / size.Width);
            var offset = Math.Ceiling(Math.Ceiling(index / capacity) * size.Height) - size.Height;
            this.SetVerticalOffset(offset);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            this.ContainerLayouts.Clear();

            var childAvailable = new Size(double.PositiveInfinity, double.PositiveInfinity);
            var childrenCount = this.InternalChildren.Count;
            var itemsControl = ItemsControl.GetItemsOwner(this);
            var x = 0.0;
            var y = 0.0;
            var lineSize = default(Size);
            var maxSize = default(Size);

            if (itemsControl != null)
            {
                childrenCount = itemsControl.Items.Count;
            }

            using (var generator = new ChildGenerator(this))
            {
                for (var position = 0; position < childrenCount; position++)
                {
                    var childSize = this.ContainerSizeForIndex(position);
                    var isWrapped = lineSize.Width + childSize.Width > availableSize.Width;
                    if (isWrapped)
                    {
                        x = 0;
                        y = y + lineSize.Height;
                    }

                    var itemRect = new Rect(x, y, childSize.Width, childSize.Height);
                    var viewportRect = new Rect(this.Offset, availableSize);
                    if (itemRect.IntersectsWith(viewportRect))
                    {
                        var child = generator.GetOrCreateChild(position);
                        if (child == null)
                        {
                            break;
                        }
                        child.Measure(childAvailable);
                        childSize = this.ContainerSizeForIndex(position);
                    }

                    this.ContainerLayouts[position] = new Rect(x, y, childSize.Width, childSize.Height);

                    isWrapped = lineSize.Width + childSize.Width > availableSize.Width;
                    if (isWrapped)
                    {
                        maxSize.Width = Math.Max(lineSize.Width, maxSize.Width);
                        maxSize.Height = maxSize.Height + lineSize.Height;
                        lineSize = childSize;

                        isWrapped = childSize.Width > availableSize.Width;
                        if (isWrapped)
                        {
                            maxSize.Width = Math.Max(childSize.Width, maxSize.Width);
                            maxSize.Height = maxSize.Height + childSize.Height;
                            lineSize = default(Size);
                        }
                    }
                    else
                    {
                        lineSize.Width = lineSize.Width + childSize.Width;
                        lineSize.Height = Math.Max(childSize.Height, lineSize.Height);
                    }

                    x = lineSize.Width;
                    y = maxSize.Height;
                }

                maxSize.Width = Math.Max(lineSize.Width, maxSize.Width);
                maxSize.Height = maxSize.Height + lineSize.Height;

                this.Extent = maxSize;
                this.Viewport = availableSize;
            }

            if (this.ScrollOwner != null)
            {
                this.ScrollOwner.InvalidateScrollInfo();
            }

            return maxSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            foreach (UIElement child in this.InternalChildren)
            {
                var itemContainerGenerator = this.ItemContainerGenerator as ItemContainerGenerator;
                var index = itemContainerGenerator.IndexFromContainer(child);
                if (!this.ContainerLayouts.ContainsKey(index))
                {
                    continue;
                }

                var layout = this.ContainerLayouts[index];
                layout.Offset(this.Offset.X * -1, this.Offset.Y * -1);
                child.Arrange(layout);
            }

            return finalSize;
        }

        protected virtual Size ContainerSizeForIndex(int index)
        {
            this.PreviousSize = this.ContainerSizeForIndexCore(index);
            return this.PreviousSize;
        }

        protected virtual Size ContainerSizeForIndexCore(int index)
        {
            var itemsOwner = ItemsControl.GetItemsOwner(this);
            var itemContainerGenerator = ItemContainerGenerator as ItemContainerGenerator;
            var item = itemContainerGenerator.ContainerFromIndex(index) as UIElement;

            if (item == null && index < itemsOwner.Items.Count)
            {
                item = itemsOwner.Items[index] as UIElement;
            }

            if (item != null)
            {
                if (item.IsMeasureValid)
                {
                    return item.DesiredSize;
                }

                if (item is FrameworkElement frameworkElement)
                {
                    if (!double.IsNaN(frameworkElement.Width) && !double.IsNaN(frameworkElement.Height))
                    {
                        return new Size(frameworkElement.Width, frameworkElement.Height);
                    }
                }
            }

            if (this.ContainerLayouts.ContainsKey(index))
            {
                return this.ContainerLayouts[index].Size;
            }

            return this.PreviousSize;
        }

        public void LineUp()
        {
            this.SetVerticalOffset(this.VerticalOffset - SystemParameters.ScrollHeight);
        }

        public void LineDown()
        {
            this.SetVerticalOffset(this.VerticalOffset + SystemParameters.ScrollHeight);
        }

        public void LineLeft()
        {
            this.SetHorizontalOffset(this.HorizontalOffset - SystemParameters.ScrollWidth);
        }

        public void LineRight()
        {
            this.SetHorizontalOffset(this.HorizontalOffset + SystemParameters.ScrollWidth);
        }

        public void PageUp()
        {
            this.SetVerticalOffset(this.VerticalOffset - this.Viewport.Height);
        }

        public void PageDown()
        {
            this.SetVerticalOffset(this.VerticalOffset + this.Viewport.Height);
        }

        public void PageLeft()
        {
            this.SetHorizontalOffset(this.HorizontalOffset - this.Viewport.Width);
        }

        public void PageRight()
        {
            this.SetHorizontalOffset(this.HorizontalOffset + this.Viewport.Width);
        }

        public void MouseWheelUp()
        {
            this.SetVerticalOffset(this.VerticalOffset - SystemParameters.ScrollHeight * SystemParameters.WheelScrollLines);
        }

        public void MouseWheelDown()
        {
            this.SetVerticalOffset(this.VerticalOffset + SystemParameters.ScrollHeight * SystemParameters.WheelScrollLines);
        }

        public void MouseWheelLeft()
        {
            this.SetHorizontalOffset(this.HorizontalOffset - SystemParameters.ScrollWidth * SystemParameters.WheelScrollLines);
        }

        public void MouseWheelRight()
        {
            this.SetHorizontalOffset(this.HorizontalOffset + SystemParameters.ScrollWidth * SystemParameters.WheelScrollLines);
        }

        public Rect MakeVisible(Visual visual, Rect rectangle)
        {
            var index = this.InternalChildren.IndexOf(visual as UIElement);

            var generator = this.ItemContainerGenerator;
            if (generator != null)
            {
                var pos = new GeneratorPosition(index, 0);
                index = generator.IndexFromGeneratorPosition(pos);
            }

            if (index < 0)
            {
                return Rect.Empty;
            }

            if (!this.ContainerLayouts.ContainsKey(index))
            {
                return Rect.Empty;
            }

            var layout = this.ContainerLayouts[index];
            if (this.HorizontalOffset + this.ViewportWidth < layout.X + layout.Width)
            {
                this.SetHorizontalOffset(layout.X + layout.Width - this.ViewportWidth);
            }
            if (layout.X < this.HorizontalOffset)
            {
                this.SetHorizontalOffset(layout.X);
            }

            if (this.VerticalOffset + this.ViewportHeight < layout.Y + layout.Height)
            {
                this.SetVerticalOffset(layout.Y + layout.Height - this.ViewportHeight);
            }
            if (layout.Y < this.VerticalOffset)
            {
                this.SetVerticalOffset(layout.Y);
            }

            layout.Width = Math.Min(this.ViewportWidth, layout.Width);
            layout.Height = Math.Min(this.ViewportHeight, layout.Height);

            return layout;
        }

        public void SetHorizontalOffset(double offset)
        {
            if (offset < 0 || this.ViewportWidth >= this.ExtentWidth)
            {
                offset = 0;
            }
            else
            {
                if (offset + this.ViewportWidth >= this.ExtentWidth)
                {
                    offset = this.ExtentWidth - this.ViewportWidth;
                }
            }

            this.Offset = new Point(offset, this.Offset.Y);
            if (this.ScrollOwner != null)
            {
                this.ScrollOwner.InvalidateScrollInfo();
            }
            this.InvalidateMeasure();
        }

        public void SetVerticalOffset(double offset)
        {
            if (offset < 0 || this.ViewportHeight >= this.ExtentHeight)
            {
                offset = 0;
            }
            else
            {
                if (offset + this.ViewportHeight >= this.ExtentHeight)
                {
                    offset = this.ExtentHeight - this.ViewportHeight;
                }
            }

            this.Offset = new Point(this.Offset.X, offset);
            if (this.ScrollOwner != null)
            {
                this.ScrollOwner.InvalidateScrollInfo();
            }
            this.InvalidateMeasure();
        }

        public class ChildGenerator : IDisposable
        {
            public ChildGenerator()
            {

            }

            public ChildGenerator(VirtualizingWrapPanel owner) : this()
            {
                this.Owner = owner;
                this.Generator = owner.ItemContainerGenerator;
            }

            public VirtualizingWrapPanel Owner { get; private set; }

            public IItemContainerGenerator Generator { get; private set; }

            public IDisposable GeneratorTracker { get; private set; }

            public int FirstGeneratedIndex { get; private set; }

            public int LastGeneratedIndex { get; private set; }

            public int CurrentGenerateIndex { get; private set; }

            public UIElement GetOrCreateChild(int index)
            {
                if (this.GeneratorTracker == null)
                {
                    var startPos = this.Generator.GeneratorPositionFromIndex(index);
                    this.FirstGeneratedIndex = index;
                    this.CurrentGenerateIndex = startPos.Offset == 0 ? startPos.Index : startPos.Index + 1;
                    this.GeneratorTracker = this.Generator.StartAt(startPos, GeneratorDirection.Forward, true);
                }

                var newlyRealized = default(bool);
                var child = this.Generator.GenerateNext(out newlyRealized) as UIElement;
                if (newlyRealized)
                {
                    if (this.CurrentGenerateIndex >= this.Owner.InternalChildren.Count)
                    {
                        if (child != null)
                        {
                            this.Owner.AddInternalChild(child);
                        }
                    }
                    else if (child != null)
                    {
                        this.Owner.InsertInternalChild(this.CurrentGenerateIndex, child);
                    }
                    this.Generator.PrepareItemContainer(child);
                }

                if (child != null)
                {
                    this.LastGeneratedIndex = index;
                    this.CurrentGenerateIndex++;
                }
                return child;
            }

            public void CleanupChildren()
            {
                if (this.Generator == null)
                {
                    return;
                }

                var children = this.Owner.InternalChildren;
                for (var i = children.Count - 1; i >= 0; i--)
                {
                    var childPos = new GeneratorPosition(i, 0);
                    var index = this.Generator.IndexFromGeneratorPosition(childPos);
                    if (index >= this.FirstGeneratedIndex && index <= this.LastGeneratedIndex)
                    {
                        continue;
                    }
                    this.Generator.Remove(childPos, 1);
                    this.Owner.RemoveInternalChildRange(i, 1);
                }
            }

            public bool IsDisposed { get; private set; }

            public void Dispose()
            {
                this.Dispose(true);
                GC.SuppressFinalize(this);
            }

            protected virtual void Dispose(bool disposing)
            {
                if (this.IsDisposed || !disposing)
                {
                    return;
                }
                this.OnDisposing();
                this.IsDisposed = true;
            }

            protected virtual void OnDisposing()
            {
                this.CleanupChildren();
                if (this.GeneratorTracker != null)
                {
                    this.GeneratorTracker.Dispose();
                }
            }

            ~ChildGenerator()
            {
                Logger.Write(typeof(ChildGenerator), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
                try
                {
                    this.Dispose(true);
                }
                catch
                {
                    //Nothing can be done, never throw on GC thread.
                }
            }
        }
    }
}
#endif