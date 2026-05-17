using FoxTunes.Interfaces;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class LibraryBrowserImageConverter : ViewModelBase, IValueConverter, IConfigurationBaseTarget
    {
        public static readonly LibraryBrowserTileBrushFactory Factory = ComponentRegistry.Instance.GetComponent<LibraryBrowserTileBrushFactory>();

        public LibraryBrowserImageConverter()
        {
            this.LibraryBrowserTile = new LibraryBrowserTileBrushFactory.LibraryBrowserTile();
        }

        public LibraryBrowserTileBrushFactory.LibraryBrowserTile LibraryBrowserTile { get; private set; }

        private int _TileSize { get; set; }

        public int TileSize
        {
            get
            {
                return this._TileSize;
            }
            set
            {
                this._TileSize = value;
                this.OnTileSizeChanged();
            }
        }

        protected virtual void OnTileSizeChanged()
        {
            this.LibraryBrowserTile.Update(this.TileSize, this.TileSize, this.ImageMode);
            if (this.TileSizeChanged != null)
            {
                this.TileSizeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("TileSize");
        }

        public event EventHandler TileSizeChanged;

        private LibraryBrowserImageMode _ImageMode { get; set; }

        public LibraryBrowserImageMode ImageMode
        {
            get
            {
                return this._ImageMode;
            }
            set
            {
                this._ImageMode = value;
                this.OnImageModeChanged();
            }
        }

        protected virtual void OnImageModeChanged()
        {
            this.LibraryBrowserTile.Update(this.TileSize, this.TileSize, this.ImageMode);
            if (this.ImageModeChanged != null)
            {
                this.ImageModeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ImageMode");
        }

        public event EventHandler ImageModeChanged;

        private IConfigurationBase _Configuration { get; set; }

        public IConfigurationBase Configuration
        {
            get
            {
                return this._Configuration;
            }
            set
            {
                this._Configuration = value;
                this.OnConfigurationChanged();
            }
        }

        protected virtual void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.Configuration.GetElement<IntegerConfigurationElement>(
                    LibraryBrowserBaseConfiguration.SECTION,
                    LibraryBrowserBaseConfiguration.TILE_SIZE
                ).ConnectValue(value => this.TileSize = value);
                this.Configuration.GetElement<SelectionConfigurationElement>(
                    LibraryBrowserBaseConfiguration.SECTION,
                    LibraryBrowserBaseConfiguration.TILE_IMAGE
                ).ConnectValue(value => this.ImageMode = LibraryBrowserBaseConfiguration.GetLibraryImage(value));
            }
            if (this.ConfigurationChanged != null)
            {
                this.ConfigurationChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Configuration");
        }

        public event EventHandler ConfigurationChanged;

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
