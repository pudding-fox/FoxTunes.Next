using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FoxTunes
{
    public class Axis : Control
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            "Orientation",
            typeof(Orientation),
            typeof(Axis),
            new PropertyMetadata(Orientation.Horizontal, new PropertyChangedCallback(OnOrientationChanged))
        );

        public static Orientation GetOrientation(Axis source)
        {
            return (Orientation)source.GetValue(OrientationProperty);
        }

        public static void SetOrientation(Axis source, Orientation value)
        {
            source.SetValue(OrientationProperty, value);
        }

        public static void OnOrientationChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var axis = sender as Axis;
            if (axis == null)
            {
                return;
            }
            axis.OnOrientationChanged();
        }

        public static readonly DependencyProperty LabelProviderProperty = DependencyProperty.Register(
            "LabelProvider",
            typeof(AxisLabelProvider),
            typeof(Axis),
            new PropertyMetadata(new PropertyChangedCallback(OnLabelProviderChanged))
        );

        public static AxisLabelProvider GetLabelProvider(Axis source)
        {
            return (AxisLabelProvider)source.GetValue(LabelProviderProperty);
        }

        public static void SetLabelProvider(Axis source, AxisLabelProvider value)
        {
            source.SetValue(LabelProviderProperty, value);
        }

        public static void OnLabelProviderChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var axis = sender as Axis;
            if (axis == null)
            {
                return;
            }
            axis.OnLabelProviderChanged();
        }

        public Orientation Orientation
        {
            get
            {
                return GetOrientation(this);
            }
            set
            {
                SetOrientation(this, value);
            }
        }

        protected virtual void OnOrientationChanged()
        {
            if (this.OrientationChanged != null)
            {
                this.OrientationChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler OrientationChanged;

        public AxisLabelProvider LabelProvider
        {
            get
            {
                return GetLabelProvider(this);
            }
            set
            {
                SetLabelProvider(this, value);
            }
        }

        protected virtual void OnLabelProviderChanged()
        {
            if (this.LabelProvider != null)
            {
                this.LabelProvider.Invalidated += this.OnInvalidated;
            }
            if (this.LabelProviderChanged != null)
            {
                this.LabelProviderChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler LabelProviderChanged;

        protected virtual void OnInvalidated(object sender, EventArgs e)
        {
            this.InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (this.LabelProvider == null)
            {
                return;
            }
            var culture = CultureInfo.CurrentCulture;
            var typeFace = new Typeface(this.FontFamily, this.FontStyle, this.FontWeight, this.FontStretch);
            var labels = this.LabelProvider.GetLabels();
#pragma warning disable 612, 618
            var formatted = labels.Select(
                label => new FormattedText(label, culture, this.FlowDirection, typeFace, this.FontSize, this.Foreground)
            ).ToArray();
#pragma warning restore 612, 618
            if (formatted.Length == 0)
            {
                return;
            }
            switch (this.Orientation)
            {
                case Orientation.Horizontal:
                    this.OnRenderHorizontal(formatted, drawingContext);
                    break;
                case Orientation.Vertical:
                    this.OnRenderVertical(formatted, drawingContext);
                    break;
            }
        }

        protected virtual void OnRenderHorizontal(FormattedText[] labels, DrawingContext drawingContext)
        {
            var width = Convert.ToInt32(this.ActualWidth);
            var y = Convert.ToInt32(this.ActualHeight / 2);
            labels = this.TrimToWidth(labels, width);
            var position = 0;
            for (float step = (float)width / labels.Length, x = step / 2; x < width && position < labels.Length; x += step, position++)
            {
                var label = labels[position];
                var origin = new Point(x - (label.Width / 2), y - (label.Height / 2));
                drawingContext.DrawText(label, origin);
            }
        }

        protected virtual FormattedText[] TrimToWidth(FormattedText[] labels, int width)
        {
            do
            {
                var total = labels.Sum(
                    label => this.Padding.Left + label.Width + this.Padding.Right
                );
                if (total <= width)
                {
                    break;
                }
                labels = labels.Where(
                    (label, index) => index % 2 == 0
                ).ToArray();
            } while (labels.Length > 2);
            return labels;
        }

        protected virtual void OnRenderVertical(FormattedText[] labels, DrawingContext drawingContext)
        {
            var height = Convert.ToInt32(this.ActualHeight);
            var x = Convert.ToInt32(this.ActualWidth / 2);
            labels = this.TrimToHeight(labels, height);
            var position = labels.Length - 1;
            for (float step = (float)height / labels.Length, y = step / 2; y < height && position >= 0; y += step, position--)
            {
                var label = labels[position];
                var origin = new Point(x - (label.Width / 2), y - (label.Height / 2));
                drawingContext.DrawText(label, origin);
            }
        }

        protected virtual FormattedText[] TrimToHeight(FormattedText[] labels, int height)
        {
            do
            {
                var total = labels.Sum(
                    label => this.Padding.Top + label.Height + this.Padding.Bottom
                );
                if (total <= height)
                {
                    break;
                }
                labels = labels.Where(
                    (label, index) => index % 2 == 0
                ).ToArray();
            } while (labels.Length > 2);
            return labels;
        }
    }

    public abstract class AxisLabelProvider : DependencyObject
    {
        public AxisLabelProvider()
        {

        }

        public abstract IEnumerable<string> GetLabels();

        protected virtual void OnInvalidated()
        {
            if (this.Invalidated == null)
            {
                return;
            }
            this.Invalidated(this, EventArgs.Empty);
        }

        public event EventHandler Invalidated;
    }

    public class TextAxisLabelProvider : AxisLabelProvider
    {
        public static readonly DependencyProperty ValuesProperty = DependencyProperty.Register(
            "Values",
            typeof(StringCollection),
            typeof(TextAxisLabelProvider),
            new PropertyMetadata(null, new PropertyChangedCallback(OnValuesChanged))
        );

        public static StringCollection GetValues(TextAxisLabelProvider source)
        {
            return (StringCollection)source.GetValue(ValuesProperty);
        }

        public static void SetValues(TextAxisLabelProvider source, StringCollection value)
        {
            source.SetValue(ValuesProperty, value);
        }

        public static void OnValuesChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var provider = sender as TextAxisLabelProvider;
            if (provider == null)
            {
                return;
            }
            provider.OnValuesChanged();
        }

        public TextAxisLabelProvider()
        {

        }

        public StringCollection Values
        {
            get
            {
                return GetValues(this);
            }
            set
            {
                SetValues(this, value);
            }
        }

        protected virtual void OnValuesChanged()
        {
            this.OnInvalidated();
            if (this.ValuesChanged != null)
            {
                this.ValuesChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler ValuesChanged;

        public override IEnumerable<string> GetLabels()
        {
            if (this.Values != null)
            {
                return this.Values;
            }
            return Enumerable.Empty<string>();
        }
    }
}