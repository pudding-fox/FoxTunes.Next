using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace FoxTunes
{
    public partial class TabGroupedPlaylist : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty PlaylistProperty = DependencyProperty.Register(
            "Playlist",
            typeof(Playlist),
            typeof(TabGroupedPlaylist),
            new PropertyMetadata(new PropertyChangedCallback(OnPlaylistChanged))
        );

        public static Playlist GetPlaylist(TabGroupedPlaylist source)
        {
            return (Playlist)source.GetValue(PlaylistProperty);
        }

        public static void SetPlaylist(TabGroupedPlaylist source, Playlist value)
        {
            source.SetValue(PlaylistProperty, value);
        }

        public static void OnPlaylistChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var tabGroupedPlaylist = sender as TabGroupedPlaylist;
            if (tabGroupedPlaylist == null)
            {
                return;
            }
            tabGroupedPlaylist.OnPlaylistChanged();
        }

        public TabGroupedPlaylist()
        {
            this.InitializeComponent();
#if NET40

#else
            VirtualizingPanel.SetIsVirtualizing(this.ListView, true);
            VirtualizingPanel.SetVirtualizationMode(this.ListView, VirtualizationMode.Recycling);
            VirtualizingPanel.SetIsVirtualizingWhenGrouping(this.ListView, true);
#endif
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

        protected virtual void OnGroupHeaderMouseDown(object sender, MouseButtonEventArgs e)
        {
            var element = sender as FrameworkElement;
            if (element == null)
            {
                return;
            }
            var group = element.DataContext as CollectionViewGroup;
            if (group == null)
            {
                return;
            }
            this.ListView.SelectedItems.Clear();
            foreach (var item in group.Items)
            {
                this.ListView.SelectedItems.Add(item);
            }
        }

        protected virtual void OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var mainScrollViewer = this.ListView.FindChild<ScrollViewer>();
            var headerScrollViewer = this.ListView.FindChild<ScrollViewer>("PART_ScrollViewer");
            if (mainScrollViewer == null || headerScrollViewer == null)
            {
                return;
            }
            headerScrollViewer.ScrollToHorizontalOffset(mainScrollViewer.HorizontalOffset);
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
