using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IPlaylistManager : IStandardManager, IInvocableComponent, IFileActionHandler, IDatabaseInitializer
    {
        Task Create(Playlist playlist);

        [Obsolete("Use the IFileActionHandler interface to handle paths.")]
        Task Create(Playlist playlist, IEnumerable<string> paths);

        Task Create(Playlist playlist, LibraryHierarchyNode libraryHierarchyNode);

        Task Create(Playlist playlist, IEnumerable<PlaylistItem> playlistItems);

        Task Remove(Playlist playlist);

        [Obsolete("Use the IFileActionHandler interface to handle paths.")]
        Task Add(Playlist playlist, IEnumerable<string> paths, bool clear);

        [Obsolete("Use the IFileActionHandler interface to handle paths.")]
        Task Insert(Playlist playlist, int index, IEnumerable<string> paths, bool clear);

        Task Add(Playlist playlist, LibraryHierarchyNode libraryHierarchyNode, bool clear);

        Task Insert(Playlist playlist, int index, LibraryHierarchyNode libraryHierarchyNode, bool clear);

        Task Add(Playlist playlist, IEnumerable<LibraryHierarchyNode> libraryHierarchyNodes, bool clear);

        Task Insert(Playlist playlist, int index, IEnumerable<LibraryHierarchyNode> libraryHierarchyNodes, bool clear);

        Task Add(Playlist playlist, IEnumerable<PlaylistItem> playlistItems, bool clear);

        Task Insert(Playlist playlist, int index, IEnumerable<PlaylistItem> playlistItems, bool clear);

        Task Move(Playlist playlist, IEnumerable<PlaylistItem> playlistItems);

        Task Move(Playlist playlist, int index, IEnumerable<PlaylistItem> playlistItems);

        Task Remove(Playlist playlist, IEnumerable<PlaylistItem> playlistItems);

        Task Crop(Playlist playlist, IEnumerable<PlaylistItem> playlistItems);

        Task Play(PlaylistItem playlistItem);

        Task Play(Playlist playlist, int sequence);

        Task Next();

        Task Next(bool wrap);

        Task Previous();

        Task Clear(Playlist playlist);

        Task<int> Sort(Playlist playlist, PlaylistColumn playlistColumn, bool descending);

        Playlist SelectedPlaylist { get; set; }

        event EventHandler SelectedPlaylistChanged;

        Playlist CurrentPlaylist { get; }

        event EventHandler CurrentPlaylistChanged;

        PlaylistItem CurrentItem { get; }

        event EventHandler CurrentItemChanged;

        PlaylistItem[] SelectedItems { get; set; }

        event EventHandler SelectedItemsChanged;

        string Filter { get; set; }

        event EventHandler FilterChanged;
    }

    public enum PlaylistQueueFlags : byte
    {
        None,
        Next,
        Reset
    }
}
