using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace FoxTunes
{
    public class PlaylistCollection : ObservableCollection<Playlist>
    {
        private const string COUNT = "Count";

        private const string INDEXER = "Item[]";

        public readonly object SyncRoot = new object();

        public PlaylistCollection(IEnumerable<Playlist> playlists) : base(playlists)
        {

        }

        public bool IsSuspended { get; private set; }

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

        public Action Reset(Playlist[] playlists)
        {
            lock (this.SyncRoot)
            {
                this.IsSuspended = true;
                try
                {
                    this.Clear();
                    this.AddRange(playlists);
                }
                finally
                {
                    this.IsSuspended = false;
                }
            }
            return () =>
            {
                this.OnPropertyChanged(new PropertyChangedEventArgs(COUNT));
                this.OnPropertyChanged(new PropertyChangedEventArgs(INDEXER));
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            };
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (this.IsSuspended)
            {
                return;
            }
            base.OnCollectionChanged(e);
        }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (this.IsSuspended)
            {
                return;
            }
            base.OnPropertyChanged(e);
        }
    }
}
