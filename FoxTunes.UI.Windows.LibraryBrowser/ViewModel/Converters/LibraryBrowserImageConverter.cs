using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class LibraryBrowserImageConverter : ViewModelBase, IValueConverter
    {
        public static readonly LibraryBrowserTileBrushFactory Factory = ComponentRegistry.Instance.GetComponent<LibraryBrowserTileBrushFactory>();

        public static readonly DependencyProperty WidthProperty = DependencyProperty.Register(
            "Width",
            typeof(double),
            typeof(LibraryBrowserImageConverter),
            new PropertyMetadata(new PropertyChangedCallback(OnWidthChanged))
        );

        public static double GetWidth(ViewModelBase source)
        {
            return global::System.Convert.ToDouble(source.GetValue(WidthProperty));
        }

        public static void SetWidth(ViewModelBase source, double value)
        {
            source.SetValue(WidthProperty, value);
        }

        public static void OnWidthChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var libraryBrowserImageConverter = sender as LibraryBrowserImageConverter;
            if (libraryBrowserImageConverter == null)
            {
                return;
            }
            libraryBrowserImageConverter.OnWidthChanged();
        }


        public static readonly DependencyProperty HeightProperty = DependencyProperty.Register(
            "Height",
            typeof(double),
            typeof(LibraryBrowserImageConverter),
            new PropertyMetadata(new PropertyChangedCallback(OnHeightChanged))
        );

        public static double GetHeight(ViewModelBase source)
        {
            return global::System.Convert.ToDouble(source.GetValue(HeightProperty));
        }

        public static void SetHeight(ViewModelBase source, double value)
        {
            source.SetValue(HeightProperty, value);
        }

        public static void OnHeightChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var libraryBrowserImageConverter = sender as LibraryBrowserImageConverter;
            if (libraryBrowserImageConverter == null)
            {
                return;
            }
            libraryBrowserImageConverter.OnHeightChanged();
        }

        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            "Mode",
            typeof(LibraryBrowserImageMode),
            typeof(LibraryBrowserImageConverter),
            new PropertyMetadata(new PropertyChangedCallback(OnModeChanged))
        );

        public static LibraryBrowserImageMode GetMode(ViewModelBase source)
        {
            return (LibraryBrowserImageMode)source.GetValue(ModeProperty);
        }

        public static void SetMode(ViewModelBase source, LibraryBrowserImageMode value)
        {
            source.SetValue(ModeProperty, value);
        }

        public static void OnModeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var libraryBrowserImageConverter = sender as LibraryBrowserImageConverter;
            if (libraryBrowserImageConverter == null)
            {
                return;
            }
            libraryBrowserImageConverter.OnModeChanged();
        }

        public LibraryBrowserImageConverter()
        {
            this.LibraryBrowserTile = new LibraryBrowserTileBrushFactory.LibraryBrowserTile();
        }

        public LibraryBrowserTileBrushFactory.LibraryBrowserTile LibraryBrowserTile { get; private set; }

        public double Width
        {
            get
            {
                return global::System.Convert.ToDouble(this.GetValue(WidthProperty));
            }
            set
            {
                this.SetValue(WidthProperty, value);
            }
        }

        protected virtual void OnWidthChanged()
        {
            this.LibraryBrowserTile.Update(this.Width, this.Height, this.Mode);
            if (this.WidthChanged != null)
            {
                this.WidthChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Width");
        }

        public event EventHandler WidthChanged;

        public double Height
        {
            get
            {
                return global::System.Convert.ToDouble(this.GetValue(HeightProperty));
            }
            set
            {
                this.SetValue(HeightProperty, value);
            }
        }

        protected virtual void OnHeightChanged()
        {
            this.LibraryBrowserTile.Update(this.Width, this.Height, this.Mode);
            if (this.HeightChanged != null)
            {
                this.HeightChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Height");
        }

        public event EventHandler HeightChanged;

        public LibraryBrowserImageMode Mode
        {
            get
            {
                return (LibraryBrowserImageMode)this.GetValue(ModeProperty);
            }
            set
            {
                this.SetValue(ModeProperty, value);
            }
        }

        protected virtual void OnModeChanged()
        {
            this.LibraryBrowserTile.Update(this.Width, this.Height, this.Mode);
            if (this.ModeChanged != null)
            {
                this.ModeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Mode");
        }

        public event EventHandler ModeChanged;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (Factory == null)
            {
                return null;
            }
            if (this.LibraryBrowserTile.IsEmpty)
            {
                return null;
            }
            if (value is LibraryHierarchyNode libraryHierarchyNode)
            {
                return Factory.Create(libraryHierarchyNode, this.LibraryBrowserTile);
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new LibraryBrowserImageConverter();
        }
    }
}
