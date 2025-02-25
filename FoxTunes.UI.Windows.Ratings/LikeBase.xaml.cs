using FoxTunes.Interfaces;
using System;
using System.Windows;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for LikeBase.xaml
    /// </summary>
    public partial class LikeBase : UIComponentBase
    {
        public static readonly DependencyProperty FileDataProperty = DependencyProperty.Register(
            "FileData",
            typeof(IFileData),
            typeof(LikeBase),
            new PropertyMetadata(new PropertyChangedCallback(OnFileDataChanged))
        );

        public static IFileData GetFileData(LikeBase source)
        {
            return (IFileData)source.GetValue(FileDataProperty);
        }

        public static void SetFileData(LikeBase source, IFileData value)
        {
            source.SetValue(FileDataProperty, value);
        }

        public static void OnFileDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var like = sender as LikeBase;
            if (like == null)
            {
                return;
            }
            like.OnFileDataChanged();
        }

        public static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent(
            "ValueChanged",
            RoutingStrategy.Bubble,
            typeof(LikeEventHandler),
            typeof(LikeBase)
        );

        public static void AddValueChangedHandler(DependencyObject source, LikeEventHandler handler)
        {
            var like = source as LikeBase;
            if (like != null)
            {
                like.AddHandler(ValueChangedEvent, handler);
            }
        }

        public static void RemoveValueChangedHandler(DependencyObject source, LikeEventHandler handler)
        {
            var like = source as LikeBase;
            if (like != null)
            {
                like.RemoveHandler(ValueChangedEvent, handler);
            }
        }

        public LikeBase()
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

        public event LikeEventHandler ValueChanged
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

        protected virtual void OnValueChanged(object sender, LikeEventArgs e)
        {
            this.RaiseEvent(e);
        }
    }

    public delegate void LikeEventHandler(object sender, LikeEventArgs e);

    public class LikeEventArgs : RoutedEventArgs
    {
        public LikeEventArgs(IFileData fileData, bool value) : base(LikeBase.ValueChangedEvent)
        {
            this.FileData = fileData;
            this.Value = value;
        }

        public IFileData FileData { get; private set; }

        public MetaDataItem MetaDataItem { get; set; }

        public bool Value { get; private set; }
    }
}
