using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FoxTunes.ViewModel
{
    internal class BrushAlphaConverter : ViewModelBase, IValueConverter
    {
        public static readonly DependencyProperty AlphaProperty = DependencyProperty.Register(
            "Alpha",
            typeof(byte),
            typeof(BrushAlphaConverter),
            new PropertyMetadata(new PropertyChangedCallback(OnAlphaChanged))
        );

        public static byte GetAlpha(BrushAlphaConverter source)
        {
            return (byte)source.GetValue(AlphaProperty);
        }

        public static void SetAlpha(BrushAlphaConverter source, byte value)
        {
            source.SetValue(AlphaProperty, value);
        }

        private static void OnAlphaChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var brushAlphaConverter = sender as BrushAlphaConverter;
            if (brushAlphaConverter == null)
            {
                return;
            }
            brushAlphaConverter.OnAlphaChanged();
        }

        public byte Alpha
        {
            get
            {
                return global::System.Convert.ToByte(this.GetValue(AlphaProperty));
            }
            set
            {
                this.SetValue(AlphaProperty, value);
            }
        }

        protected virtual void OnAlphaChanged()
        {
            if (this.AlphaChanged != null)
            {
                this.AlphaChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Alpha");
        }

        public event EventHandler AlphaChanged;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is SolidColorBrush brush)
            {
                var color = brush.Color;
                return new SolidColorBrush(Color.FromArgb(this.Alpha, color.R, color.G, color.B));
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new BrushAlphaConverter();
        }
    }
}
