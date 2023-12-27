namespace FoxTunes.Interfaces
{
    public interface IStandardManagers
    {
        IPlaybackManager Playback { get; }

        IPlaylistManager Playlist { get; }

        IPlaylistColumnManager PlaylistColumn { get; }

        ILibraryManager Library { get; }

        IHierarchyManager Hierarchy { get; }

        IMetaDataManager MetaData { get; }

        IMetaDataProviderManager MetaDataProvider { get; }

        IFileActionHandlerManager FileActionHandler { get; }

        IOutputDeviceManager OutputDevice { get; }
    }
}
