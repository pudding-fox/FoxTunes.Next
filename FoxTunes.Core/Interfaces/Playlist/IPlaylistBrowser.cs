using System;

namespace FoxTunes.Interfaces
{
    public interface IPlaylistBrowser : IStandardComponent
    {
        PlaylistBrowserState State { get; }

        event EventHandler StateChanged;

        PlaylistColumn[] GetColumns();

        Playlist[] GetPlaylists();

        Playlist GetPlaylist(PlaylistItem playlistItem);

        PlaylistItem[] GetItems(Playlist playlist);

        PlaylistItem[] GetItems(Playlist playlist, string filter);

        PlaylistItem GetItemById(Playlist playlist, int id);

        PlaylistItem GetItemBySequence(Playlist playlist, int sequence);

        PlaylistItem GetFirstItem(Playlist playlist);

        PlaylistItem GetLastItem(Playlist playlist);

        PlaylistItem GetNextItem(Playlist playlist, bool wrap);

        PlaylistItem GetNextItem(PlaylistItem playlistItem, bool wrap);

        PlaylistItem GetPreviousItem(Playlist playlist, bool wrap);

        PlaylistItem GetPreviousItem(PlaylistItem playlistItem, bool wrap);

        int GetInsertIndex(Playlist playlist);
    }

    public enum PlaylistBrowserState : byte
    {
        None,
        Loading
    }
}
