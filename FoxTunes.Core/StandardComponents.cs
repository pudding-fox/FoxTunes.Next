﻿using FoxTunes.Interfaces;

namespace FoxTunes
{
    public class StandardComponents : IStandardComponents
    {
        private StandardComponents()
        {

        }

        public IConfiguration Configuration
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IConfiguration>();
            }
        }

        public IUserInterface UserInterface
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IUserInterface>();
            }
        }

        public IOutput Output
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IOutput>();
            }
        }

        public IOutputDataSource OutputDataSource
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IOutputDataSource>();
            }
        }

        public IVisualizationDataSource VisualizationDataSource
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IVisualizationDataSource>();
            }
        }

        public IOutputEffects OutputEffects
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IOutputEffects>();
            }
        }

        public IOutputStreamQueue OutputStreamQueue
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IOutputStreamQueue>();
            }
        }

        public IScriptingRuntime ScriptingRuntime
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IScriptingRuntime>();
            }
        }

        public ISignalEmitter SignalEmitter
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<ISignalEmitter>();
            }
        }

        public IErrorEmitter ErrorEmitter
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IErrorEmitter>();
            }
        }

        public ILibraryBrowser LibraryBrowser
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<ILibraryBrowser>();
            }
        }

        public ILibraryCache LibraryCache
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<ILibraryCache>();
            }
        }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<ILibraryHierarchyBrowser>();
            }
        }

        public ILibraryHierarchyCache LibraryHierarchyCache
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<ILibraryHierarchyCache>();
            }
        }

        public IArtworkProvider ArtworkProvider
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IArtworkProvider>();
            }
        }

        public IFileSystemBrowser FileSystemBrowser
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IFileSystemBrowser>();
            }
        }

        public IMetaDataBrowser MetaDataBrowser
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IMetaDataBrowser>();
            }
        }

        public IMetaDataCache MetaDataCache
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IMetaDataCache>();
            }
        }

        public IMetaDataProviderCache MetaDataProviderCache
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IMetaDataProviderCache>();
            }
        }

        public IMetaDataSynchronizer MetaDataSynchronizer
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IMetaDataSynchronizer>();
            }
        }

        public IOnDemandMetaDataProvider OnDemandMetaDataProvider
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IOnDemandMetaDataProvider>();
            }
        }

        public IPlaylistBrowser PlaylistBrowser
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IPlaylistBrowser>();
            }
        }

        public IPlaylistCache PlaylistCache
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IPlaylistCache>();
            }
        }

        public IPlaylistQueue PlaylistQueue
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IPlaylistQueue>();
            }
        }

        public IFilterParser FilterParser
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IFilterParser>();
            }
        }

        public ISortParser SortParser
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<ISortParser>();
            }
        }

        public IReportEmitter ReportEmitter
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IReportEmitter>();
            }
        }

        public IBackgroundTaskEmitter BackgroundTaskEmitter
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IBackgroundTaskEmitter>();
            }
        }

        public static readonly IStandardComponents Instance = new StandardComponents();
    }
}
