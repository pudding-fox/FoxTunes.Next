using FoxTunes.Interfaces;

namespace FoxTunes
{
    public class StandardManagers : IStandardManagers
    {
        private StandardManagers()
        {

        }

        public IPlaybackManager Playback
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IPlaybackManager>();
            }
        }

        public IPlaylistManager Playlist
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IPlaylistManager>();
            }
        }

        public IPlaylistColumnManager PlaylistColumn
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IPlaylistColumnManager>();
            }
        }

        public ILibraryManager Library
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<ILibraryManager>();
            }
        }

        public IHierarchyManager Hierarchy
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IHierarchyManager>();
            }
        }

        public IMetaDataManager MetaData
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IMetaDataManager>();
            }
        }

        public IMetaDataProviderManager MetaDataProvider
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IMetaDataProviderManager>();
            }
        }

        public IFileActionHandlerManager FileActionHandler
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IFileActionHandlerManager>();
            }
        }

        public IOutputDeviceManager OutputDevice
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IOutputDeviceManager>();
            }
        }

        public static readonly IStandardManagers Instance = new StandardManagers();
    }
}
