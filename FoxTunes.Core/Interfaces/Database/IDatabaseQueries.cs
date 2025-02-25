using FoxDb.Interfaces;

namespace FoxTunes.Interfaces
{
    public interface IDatabaseQueries : IBaseComponent
    {
        IDatabaseQuery AddLibraryHierarchyNode { get; }

        IDatabaseQuery AddLibraryHierarchyNodeToPlaylist(string filter, string sort);

        IDatabaseQuery AddLibraryMetaDataItem { get; }

        IDatabaseQuery AddPlaylistMetaDataItem { get; }

        IDatabaseQuery AddSearchToPlaylist(string filter, string sort, int limit);

        IDatabaseQuery ClearLibraryMetaDataItems { get; }

        IDatabaseQuery ClearPlaylistMetaDataItems { get; }

        IDatabaseQuery GetLibraryMetaData { get; }

        IDatabaseQuery GetLibraryItemMetaData { get; }

        IDatabaseQuery GetLibraryHierarchyMetaData(string filter, int limit);

        IDatabaseQuery GetLibraryHierarchyNodes(string filter);

        IDatabaseQuery GetLibraryItems(string filter);

        IDatabaseQuery GetPlaylistItems(string filter);

        IDatabaseQuery GetOrAddMetaDataItem { get; }

        IDatabaseQuery GetPlaylistMetaData(int count, int limit);

        IDatabaseQuery RemoveCancelledLibraryItems { get; }

        IDatabaseQuery RemoveLibraryHierarchyItems { get; }

        IDatabaseQuery RemoveLibraryItems { get; }

        IDatabaseQuery RemovePlaylistItems { get; }

        IDatabaseQuery SequencePlaylistItems(string sort);

        IDatabaseQuery UpdateLibraryHierarchyNode { get; }

        IDatabaseQuery UpdateLibraryVariousArtists { get; }

        IDatabaseQuery RemoveLibraryVariousArtists { get; }

        IDatabaseQuery UpdatePlaylistVariousArtists { get; }

        IDatabaseQuery RemovePlaylistVariousArtists { get; }
    }
}
