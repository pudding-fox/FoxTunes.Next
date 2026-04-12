using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace FoxTunes
{
    public partial class TabPlaylist : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty PlaylistProperty = DependencyProperty.Register(
            "Playlist",
            typeof(Playlist),
            typeof(TabPlaylist),
            new PropertyMetadata(new PropertyChangedCallback(OnPlaylistChanged))
        );

        public static Playlist GetPlaylist(TabPlaylist source)
        {
            return (Playlist)source.GetValue(PlaylistProperty);
        }

        public static void SetPlaylist(TabPlaylist source, Playlist value)
        {
            source.SetValue(PlaylistProperty, value);
        }

        public static void OnPlaylistChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var tabPlaylist = sender as TabPlaylist;
            if (tabPlaylist == null)
            {
                return;
            }
            tabPlaylist.OnPlaylistChanged();
        }

        public TabPlaylist()
        {
            this.InitializeComponent();
        }

        public Playlist Playlist
        {
            get
            {
                return this.GetValue(PlaylistProperty) as Playlist;
            }
            set
            {
                this.SetValue(PlaylistProperty, value);
            }
        }

        protected virtual void OnPlaylistChanged()
        {
            if (this.PlaylistChanged != null)
            {
                this.PlaylistChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Playlist");
        }

        public event EventHandler PlaylistChanged;

        protected virtual void DragSourceInitialized(object sender, ListViewExtensions.DragSourceInitializedEventArgs e)
        {
            var items = (e.Data as IEnumerable)
                .OfType<PlaylistItem>()
                .ToArray();
            if (!items.Any())
            {
                return;
            }
            DragDrop.DoDragDrop(
                this.ListView,
                items,
                DragDropEffects.Copy
            );
        }

        protected virtual void OnHeaderClick(object sender, RoutedEventArgs e)
        {
            var columnHeader = e.OriginalSource as GridViewColumnHeader;
            if (columnHeader == null)
            {
                return;
            }
            var column = columnHeader.Column as PlaylistGridViewColumn;
            if (column == null || column.PlaylistColumn == null)
            {
                return;
            }
            var viewModel = this.FindResource<global::FoxTunes.ViewModel.GridPlaylist>("ViewModel");
            if (viewModel == null)
            {
                return;
            }
            var task = viewModel.Sort(column.PlaylistColumn);
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged == null)
            {
                return;
            }
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
