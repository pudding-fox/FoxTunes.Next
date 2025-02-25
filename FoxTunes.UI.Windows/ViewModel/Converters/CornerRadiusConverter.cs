using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class CornerRadiusConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CornerRadius cornerRadius && parameter is CornerRadiusMask cornerRadiusMask)
            {
                var result = default(CornerRadius);
                if (cornerRadiusMask.HasFlag(CornerRadiusMask.TopLeft))
                {
                    result.TopLeft = cornerRadius.TopLeft;
                }
                if (cornerRadiusMask.HasFlag(CornerRadiusMask.TopRight))
                {
                    result.TopRight = cornerRadius.TopRight;
                }
                if (cornerRadiusMask.HasFlag(CornerRadiusMask.BottomRight))
                {
                    result.BottomRight = cornerRadius.BottomRight;
                }
                if (cornerRadiusMask.HasFlag(CornerRadiusMask.BottomLeft))
                {
                    result.BottomLeft = cornerRadius.BottomLeft;
                }
                return result;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    [Flags]
    public enum CornerRadiusMask : byte
    {
        None = 0,
        TopLeft = 1,
        TopRight = 2,
        BottomRight = 4,
        BottomLeft = 8,
        Top = TopLeft | TopRight,
        Bottom = BottomRight | BottomLeft,
        Left = TopLeft | BottomLeft,
        Right = BottomLeft | BottomRight,
        All = TopLeft | TopRight | BottomRight | BottomLeft
    }
}
