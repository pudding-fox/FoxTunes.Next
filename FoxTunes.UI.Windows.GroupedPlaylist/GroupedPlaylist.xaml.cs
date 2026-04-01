using System.Collections;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for GroupedPlaylist.xaml
    /// </summary>
    [UIComponent(ID, role: UIComponentRole.Playlist)]
    public partial class GroupedPlaylist : UIComponentBase
    {
        public const string ID = "9A220ABE-761A-4596-8DFB-0C84DB8DBABB";

        public GroupedPlaylist()
        {
            this.InitializeComponent();
#if NET40

#else
            VirtualizingPanel.SetIsVirtualizing(this.ListView, true);
            VirtualizingPanel.SetVirtualizationMode(this.ListView, VirtualizationMode.Recycling);
            VirtualizingPanel.SetIsVirtualizingWhenGrouping(this.ListView, true);
#endif
        }

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
    }
}
