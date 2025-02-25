using System.Collections.Generic;

namespace FoxTunes
{
    public class PlaylistCollection : ObservableCollection<Playlist>
    {
        public PlaylistCollection(IEnumerable<Playlist> playlists) : base(playlists)
        {

        }

        public void AddOrUpdate(IEnumerable<Playlist> playlists)
        {
            foreach (var playlist in playlists)
            {
                var index = this.IndexOf(playlist);
                if (index < 0)
                {
                    this.Add(playlist);
                }
                else
                {
                    this[index].Sequence = playlist.Sequence;
                    this[index].Name = playlist.Name;
                    this[index].Type = playlist.Type;
                    this[index].Config = playlist.Config;
                    this[index].Enabled = playlist.Enabled;
                }
            }
        }

        public void Remove(IEnumerable<Playlist> playlists)
        {
            foreach (var playlist in playlists)
            {
                this.Remove(playlist);
            }
        }
    }
}
