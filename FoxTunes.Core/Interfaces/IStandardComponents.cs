namespace FoxTunes.Interfaces
{
    public interface IStandardComponents
    {
        IConfiguration Configuration { get; }

        IUserInterface UserInterface { get; }

        IOutput Output { get; }

        IOutputDataSource OutputDataSource { get; }

        IVisualizationDataSource VisualizationDataSource { get; }

        IOutputEffects OutputEffects { get; }

        IOutputStreamQueue OutputStreamQueue { get; }

        IScriptingRuntime ScriptingRuntime { get; }

        ISignalEmitter SignalEmitter { get; }

        IErrorEmitter ErrorEmitter { get; }

        ILibraryBrowser LibraryBrowser { get; }

        ILibraryCache LibraryCache { get; }

        ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; }

        ILibraryHierarchyCache LibraryHierarchyCache { get; }

        IArtworkProvider ArtworkProvider { get; }

        IFileSystemBrowser FileSystemBrowser { get; }

        IMetaDataBrowser MetaDataBrowser { get; }

        IMetaDataCache MetaDataCache { get; }

        IMetaDataProviderCache MetaDataProviderCache { get; }

        IMetaDataSynchronizer MetaDataSynchronizer { get; }

        IOnDemandMetaDataProvider OnDemandMetaDataProvider { get; }

        IPlaylistBrowser PlaylistBrowser { get; }

        IPlaylistCache PlaylistCache { get; }

        IPlaylistQueue PlaylistQueue { get; }

        IFilterParser FilterParser { get; }

        ISortParser SortParser { get; }

        IReportEmitter ReportEmitter { get; }

        IBackgroundTaskEmitter BackgroundTaskEmitter { get; }
    }
}
