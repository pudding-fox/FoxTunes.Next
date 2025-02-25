using FoxTunes.Interfaces;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for ArtworkStack.xaml
    /// </summary>
    public partial class ArtworkStack : UserControl
    {
        public static readonly DependencyProperty FileDataProperty = DependencyProperty.Register(
            "FileData",
            typeof(IFileData),
            typeof(ArtworkStack),
            new PropertyMetadata(new PropertyChangedCallback(OnFileDataChanged))
        );

        public static IFileData GetFileData(ArtworkStack source)
        {
            return (IFileData)source.GetValue(FileDataProperty);
        }

        public static void SetFileData(ArtworkStack source, IFileData value)
        {
            source.SetValue(FileDataProperty, value);
        }

        public static void OnFileDataChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var artworkStack = sender as ArtworkStack;
            if (artworkStack == null)
            {
                return;
            }
            artworkStack.OnFileDataChanged();
        }

        public ArtworkStack()
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
    }
}
