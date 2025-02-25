using FoxTunes.Interfaces;
using System;
using System.Windows;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for RatingBase.xaml
    /// </summary>
    public partial class RatingBase : UIComponentBase
    {
        public static readonly DependencyProperty FileDataProperty = DependencyProperty.Register(
            "FileData",
            typeof(IFileData),
            typeof(RatingBase),
            new PropertyMetadata(new PropertyChangedCallback(OnFileDataChanged))
        );

        public static IFileData GetFileData(RatingBase source)
        {
            return (IFileData)source.GetValue(FileDataProperty);
        }

        public static void SetFileData(RatingBase source, IFileData value)
        {
            source.SetValue(FileDataProperty, value);
        }

        public static void OnFileDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var rating = sender as RatingBase;
            if (rating == null)
            {
                return;
            }
            rating.OnFileDataChanged();
        }

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged",
            RoutingStrategy.Bubble,
            typeof(RatingEventHandler),
            typeof(RatingBase)
        );

        public static void AddValueChangedHandler(DependencyObject source, RatingEventHandler handler)
        {
            var rating = source as RatingBase;
            if (rating != null)
            {
                rating.AddHandler(ValueChangedEvent, handler);
            }
        }

        public static void RemoveValueChangedHandler(DependencyObject source, RatingEventHandler handler)
        {
            var rating = source as RatingBase;
            if (rating != null)
            {
                rating.RemoveHandler(ValueChangedEvent, handler);
            }
        }

        public RatingBase()
        {
            this.InitializeComponent();
        }

        public IFileData FileData
        {
            get
            {
                return GetFileData(this);
            }
            set
            {
                SetFileData(this, value);
            }
        }

        protected virtual void OnFileDataChanged()
        {
            if (this.FileDataChanged != null)
            {
                this.FileDataChanged(this, EventArgs.Empty);
            }
        }

        public event EventHandler FileDataChanged;

        public event RatingEventHandler ValueChanged
        {
            add
            {
                AddValueChangedHandler(this, value);
            }
            remove
            {
                RemoveValueChangedHandler(this, value);
            }
        }

        protected virtual void OnValueChanged(object sender, RatingEventArgs e)
        {
            this.RaiseEvent(e);
        }
    }

    public delegate void RatingEventHandler(object sender, RatingEventArgs e);

    public class RatingEventArgs : RoutedEventArgs
    {
        public RatingEventArgs(IFileData fileData, byte value) : base(RatingBase.ValueChangedEvent)
        {
            this.FileData = fileData;
            this.Value = value;
        }

        public IFileData FileData { get; private set; }

        public MetaDataItem MetaDataItem { get; set; }

        public byte Value { get; private set; }
    }
}
