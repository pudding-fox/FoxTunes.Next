using System;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Genres.xaml
    /// </summary>
    public partial class Genres : UserControl
    {
        public static readonly DependencyProperty SelectedGenresProperty = DependencyProperty.Register(
            "SelectedGenres",
            typeof(string),
            typeof(Genres),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnSelectedGenresPropertyChanged))
        );

        public static string GetSelectedGenres(Genres source)
        {
            return (string)source.GetValue(SelectedGenresProperty);
        }

        public static void SetSelectedGenres(Genres source, string value)
        {
            source.SetValue(SelectedGenresProperty, value);
        }

        private static void OnSelectedGenresPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var genres = sender as Genres;
            if (genres == null)
            {
                return;
            }
            genres.OnSelectedGenresChanged();
        }

        public Genres()
        {
            this.InitializeComponent();
        }

        public string SelectedGenres
        {
            get
            {
                return GetSelectedGenres(this);
            }
            set
            {
                SetSelectedGenres(this, value);
            }
        }

        protected virtual void OnSelectedGenresChanged()
        {
            if (this.SelectedGenresChanged == null)
            {
                return;
            }
            this.SelectedGenresChanged(this, EventArgs.Empty);
        }

        public event EventHandler SelectedGenresChanged;
    }
}
