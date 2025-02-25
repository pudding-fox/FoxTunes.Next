using System.ComponentModel;
using System.Windows.Controls;

namespace FoxTunes
{
    public class PlaylistGridViewColumn : GridViewColumn
    {
        public PlaylistGridViewColumn(PlaylistColumn playlistColumn)
        {
            this.PlaylistColumn = playlistColumn;
            this.Header = playlistColumn.Name;
        }

        public PlaylistColumn PlaylistColumn { get; private set; }

        public void Refresh()
        {
            this.OnPropertyChanged(new PropertyChangedEventArgs("DisplayMemberBinding"));
        }
    }
}
