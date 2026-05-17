//Content from https://github.com/sbaeumlisberger/VirtualizingWrapPanel
#if NET40
# else
#pragma warning disable 612, 618
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace FoxTunes
{
    /// <summary>
    /// A implementation of a wrap panel that supports virtualization and can be used in horizontal and vertical orientation.
    /// <p class="note">In order to work properly all items must have the same size.</p>
    /// </summary>
    public class VirtualizingWrapPanel : VirtualizingPanelBase
    {
        public static readonly DependencyProperty ItemSizeProperty = DependencyProperty.Register(nameof(ItemSize), typeof(Size), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(Size.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public static readonly DependencyProperty StretchItemsProperty = DependencyProperty.Register(nameof(StretchItems), typeof(bool), typeof(VirtualizingWrapPanel), new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// Gets or sets a value that specifies the size of the items. The default value is <see cref="Size.Empty"/>. 
        /// If the value is <see cref="Size.Empty"/> the size of the items gots measured by the first realized item.
        /// </summary>
        public Size ItemSize { get => (Size)GetValue(ItemSizeProperty); set => SetValue(ItemSizeProperty, value); }

        /// <summary>
        /// Gets or sets a value that specifies if the items get stretched to fill up remaining space. The default value is false.
        /// </summary>
        /// <remarks>
        /// The MaxWidth and MaxHeight properties of the ItemContainerStyle can be used to limit the stretching. 
        /// In this case the use of the remaining space will be determined by the SpacingMode property. 
        /// </remarks>
        public bool StretchItems { get => (bool)GetValue(StretchItemsProperty); set => SetValue(StretchItemsProperty, value); }

        protected Size childSize;

        protected int rowCount;

        protected int itemsPerRowCount;

        protected override Size MeasureOverride(Size availableSize)
        {
            UpdateChildSize(availableSize);
            return base.MeasureOverride(availableSize);
        }

        private void UpdateChildSize(Size availableSize)
        {
            if (ItemSize != Size.Empty)
            {
                childSize = ItemSize;
            }
            else if (InternalChildren.Count != 0)
            {
                childSize = InternalChildren[0].DesiredSize;
            }
            else
            {
                childSize = CalculateChildSize(availableSize);
            }

            if (double.IsInfinity(GetWidth(availableSize)))
            {
                itemsPerRowCount = Items.Count;
            }
            else
            {
                itemsPerRowCount = Math.Max(1, (int)Math.Floor(GetWidth(availableSize) / GetWidth(childSize)));
            }

            rowCount = (int)Math.Ceiling((double)Items.Count / itemsPerRowCount);
        }

        private Size CalculateChildSize(Size availableSize)
        {
            if (Items.Count == 0)
            {
                return new Size(0, 0);
            }
            var startPosition = ItemContainerGenerator.GeneratorPositionFromIndex(0);
            using (ItemContainerGenerator.StartAt(startPosition, GeneratorDirection.Forward, true))
            {
                var child = (UIElement)ItemContainerGenerator.GenerateNext();
                AddInternalChild(child);
                ItemContainerGenerator.PrepareItemContainer(child);
                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                return child.DesiredSize;
            }
        }

        protected override Size CalculateExtent(Size availableSize)
        {
            double extentWidth = !double.IsInfinity(GetWidth(availableSize))
                ? GetWidth(availableSize)
                : GetWidth(childSize) * itemsPerRowCount;

            double extentHeight = GetHeight(childSize) * rowCount;
            return CreateSize(extentWidth, extentHeight);
        }

        protected void CalculateSpacing(Size finalSize, out double innerSpacing, out double outerSpacing)
        {
            Size childSize = CalculateChildArrangeSize(finalSize);

            double finalWidth = GetWidth(finalSize);

            double totalItemsWidth = Math.Min(GetWidth(childSize) * itemsPerRowCount, finalWidth);
            double unusedWidth = finalWidth - totalItemsWidth;

            innerSpacing = outerSpacing = unusedWidth / (itemsPerRowCount + 1);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            double offsetX = GetX(Offset);
            double offsetY = GetY(Offset);

            Size childSize = CalculateChildArrangeSize(finalSize);

            CalculateSpacing(finalSize, out double innerSpacing, out double outerSpacing);

            for (int childIndex = 0; childIndex < InternalChildren.Count; childIndex++)
            {
                UIElement child = InternalChildren[childIndex];

                int itemIndex = GetItemIndexFromChildIndex(childIndex);

                int columnIndex = itemIndex % itemsPerRowCount;
                int rowIndex = itemIndex / itemsPerRowCount;

                double x = outerSpacing + columnIndex * (GetWidth(childSize) + innerSpacing);
                double y = rowIndex * GetHeight(childSize);

                if (GetHeight(finalSize) == 0.0)
                {
                    /* When the parent panel is grouping and a cached group item is not 
                     * in the viewport it has no valid arrangement. That means that the 
                     * height/width is 0. Therefore the items should not be visible so 
                     * that they are not falsely displayed. */
                    child.Arrange(new Rect(0, 0, 0, 0));
                }
                else
                {
                    child.Arrange(CreateRect(x - offsetX, y - offsetY, childSize.Width, childSize.Height));
                }
            }

            return finalSize;
        }

        protected Size CalculateChildArrangeSize(Size finalSize)
        {
            if (StretchItems)
            {
                double childMaxWidth = ReadItemContainerStyle(MaxWidthProperty, double.PositiveInfinity);
                double maxPossibleChildWith = finalSize.Width / itemsPerRowCount;
                double childWidth = Math.Min(maxPossibleChildWith, childMaxWidth);
                return new Size(childWidth, childSize.Height);
            }
            else
            {
                return childSize;
            }
        }

        private T ReadItemContainerStyle<T>(DependencyProperty property, T fallbackValue) where T : notnull
        {
            var value = ItemsControl.ItemContainerStyle?.Setters.OfType<Setter>()
                .FirstOrDefault(setter => setter.Property == property)?.Value;
            return (T)(value ?? fallbackValue);
        }

        protected override ItemRange UpdateItemRange()
        {
            if (!IsVirtualizing)
            {
                return new ItemRange(0, Items.Count - 1);
            }

            int startIndex;
            int endIndex;

            double viewportSartPos = GetY(Offset);
            double viewportEndPos = GetY(Offset) + GetHeight(Viewport);

            if (CacheLengthUnit == VirtualizationCacheLengthUnit.Pixel)
            {
                viewportSartPos = Math.Max(viewportSartPos - CacheLength.CacheBeforeViewport, 0);
                viewportEndPos = Math.Min(viewportEndPos + CacheLength.CacheAfterViewport, GetHeight(Extent));
            }

            int startRowIndex = GetRowIndex(viewportSartPos);
            startIndex = startRowIndex * itemsPerRowCount;

            int endRowIndex = GetRowIndex(viewportEndPos);
            endIndex = Math.Min(endRowIndex * itemsPerRowCount + (itemsPerRowCount - 1), Items.Count - 1);

            if (CacheLengthUnit == VirtualizationCacheLengthUnit.Page)
            {
                int itemsPerPage = endIndex - startIndex + 1;
                startIndex = Math.Max(startIndex - (int)CacheLength.CacheBeforeViewport * itemsPerPage, 0);
                endIndex = Math.Min(endIndex + (int)CacheLength.CacheAfterViewport * itemsPerPage, Items.Count - 1);
            }
            else if (CacheLengthUnit == VirtualizationCacheLengthUnit.Item)
            {
                startIndex = Math.Max(startIndex - (int)CacheLength.CacheBeforeViewport, 0);
                endIndex = Math.Min(endIndex + (int)CacheLength.CacheAfterViewport, Items.Count - 1);
            }

            return new ItemRange(startIndex, endIndex);
        }

        private int GetRowIndex(double location)
        {
            int calculatedRowIndex = (int)Math.Floor(location / GetHeight(childSize));
            int maxRowIndex = (int)Math.Ceiling((double)Items.Count / (double)itemsPerRowCount);
            return Math.Max(Math.Min(calculatedRowIndex, maxRowIndex), 0);
        }

        protected override void BringIndexIntoView(int index)
        {
            if (index < 0 || index >= Items.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"The argument {nameof(index)} must be >= 0 and < the number of items.");
            }

            if (itemsPerRowCount == 0)
            {
                throw new InvalidOperationException();
            }

            var offset = (index / itemsPerRowCount) * GetHeight(childSize);

            SetVerticalOffset(offset);
        }

        protected override double GetLineUpScrollAmount()
        {
            return -Math.Min(childSize.Height * ScrollLineDeltaItem, Viewport.Height);
        }

        protected override double GetLineDownScrollAmount()
        {
            return Math.Min(childSize.Height * ScrollLineDeltaItem, Viewport.Height);
        }

        protected override double GetLineLeftScrollAmount()
        {
            return -Math.Min(childSize.Width * ScrollLineDeltaItem, Viewport.Width);
        }

        protected override double GetLineRightScrollAmount()
        {
            return Math.Min(childSize.Width * ScrollLineDeltaItem, Viewport.Width);
        }

        protected override double GetMouseWheelUpScrollAmount()
        {
            return -Math.Min(childSize.Height * MouseWheelDeltaItem, Viewport.Height);
        }

        protected override double GetMouseWheelDownScrollAmount()
        {
            return Math.Min(childSize.Height * MouseWheelDeltaItem, Viewport.Height);
        }

        protected override double GetMouseWheelLeftScrollAmount()
        {
            return -Math.Min(childSize.Width * MouseWheelDeltaItem, Viewport.Width);
        }

        protected override double GetMouseWheelRightScrollAmount()
        {
            return Math.Min(childSize.Width * MouseWheelDeltaItem, Viewport.Width);
        }

        protected override double GetPageUpScrollAmount()
        {
            return -Viewport.Height;
        }

        protected override double GetPageDownScrollAmount()
        {
            return Viewport.Height;
        }

        protected override double GetPageLeftScrollAmount()
        {
            return -Viewport.Width;
        }

        protected override double GetPageRightScrollAmount()
        {
            return Viewport.Width;
        }

        /* orientation aware helper methods */

        protected double GetX(Point point) => point.X;
        protected double GetY(Point point) => point.Y;

        protected double GetWidth(Size size) => size.Width;
        protected double GetHeight(Size size) => size.Height;

        protected Size CreateSize(double width, double height) => new Size(width, height);
        protected Rect CreateRect(double x, double y, double width, double height) => new Rect(x, y, width, height);
    }
}
#endif