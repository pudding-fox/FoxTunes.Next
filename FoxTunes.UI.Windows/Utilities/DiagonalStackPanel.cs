using System;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public class DiagonalStackPanel : Panel
    {
        public static readonly DependencyProperty XStepProperty = DependencyProperty.Register(
            "XStep",
            typeof(double),
            typeof(DiagonalStackPanel),
            new FrameworkPropertyMetadata() { AffectsArrange = true, AffectsMeasure = true }
        );

        public static double GetXStep(UIElement owner)
        {
            return (double)owner.GetValue(XStepProperty);
        }

        public static void SetXStep(UIElement owner, double value)
        {
            owner.SetValue(XStepProperty, value);
        }

        public static readonly DependencyProperty YStepProperty = DependencyProperty.Register(
            "YStep",
            typeof(double),
            typeof(DiagonalStackPanel)
        );

        public static double GetYStep(UIElement owner)
        {
            return (double)owner.GetValue(YStepProperty);
        }

        public static void SetYStep(UIElement owner, double value)
        {
            owner.SetValue(YStepProperty, value);
        }

        public static readonly DependencyProperty ReverseProperty = DependencyProperty.Register(
            "Reverse",
            typeof(bool),
            typeof(DiagonalStackPanel)
        );

        public static bool GetReverse(UIElement owner)
        {
            return (bool)owner.GetValue(ReverseProperty);
        }

        public static void SetReverse(UIElement owner, bool value)
        {
            owner.SetValue(ReverseProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            var size = new Size();
            for (var a = 0; a < this.InternalChildren.Count; a++)
            {
                var child = this.InternalChildren[a];
                child.Measure(availableSize);
                var rect = this.GetChildRect(a, child);
                size.Width = Math.Max(size.Width, rect.X + rect.Width);
                size.Height = Math.Max(size.Height, rect.Y + rect.Height);
            }
            return size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (GetReverse(this))
            {
                for (int a = this.InternalChildren.Count - 1, b = 0; a >= 0; a--, b++)
                {
                    var child = this.InternalChildren[a];
                    var rect = this.GetChildRect(b, child);
                    child.Arrange(rect);
                }
            }
            else
            {
                for (var a = 0; a < this.InternalChildren.Count; a++)
                {
                    var child = this.InternalChildren[a];
                    var rect = this.GetChildRect(a, child);
                    child.Arrange(rect);
                }
            }
            return base.ArrangeOverride(finalSize);
        }

        private Rect GetChildRect(int index, UIElement child)
        {
            var xStep = GetXStep(this);
            var yStep = GetYStep(this);
            return new Rect(xStep * index, yStep * index, child.DesiredSize.Width, child.DesiredSize.Height);
        }
    }
}
