using FoxTunes.Interfaces;
using FoxTunes.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace FoxTunes
{
    //Setting PRIORITY_HIGH so the the cache is cleared before being re-queried.
    [ComponentPriority(ComponentPriorityAttribute.HIGH)]
    [WindowsUserInterfaceDependency]
    //TODO: This was (and technically still is) a StandardFactory, but it overrides the ComponentPriorityAttribute.HIGH.
    //TODO: I hate the Factory, Manager, Behaviour and Component sub types and they should go away...
    public class LibraryBrowserTileBrushFactory : StandardComponent, IDisposable
    {
        public LibraryBrowserTileProvider LibraryBrowserTileProvider { get; private set; }

        public ImageLoader ImageLoader { get; private set; }

        public ArtworkPlaceholderBrushFactory PlaceholderBrushFactory { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public TaskFactory Factory { get; private set; }

        public ImageBrushCache<Tuple<LibraryHierarchyNode, LibraryBrowserImageMode>> Store { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.LibraryBrowserTileProvider = ComponentRegistry.Instance.GetComponent<LibraryBrowserTileProvider>();
            this.ImageLoader = ComponentRegistry.Instance.GetComponent<ImageLoader>();
            this.PlaceholderBrushFactory = ComponentRegistry.Instance.GetComponent<ArtworkPlaceholderBrushFactory>();
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<IntegerConfigurationElement>(
                ImageBehaviourConfiguration.SECTION,
                ImageLoaderConfiguration.THREADS
            ).ConnectValue(value => this.CreateTaskFactory(value));
            this.Configuration.GetElement<IntegerConfigurationElement>(
                ImageBehaviourConfiguration.SECTION,
                ImageLoaderConfiguration.CACHE_SIZE
            ).ConnectValue(value => this.CreateCache(value));
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.MetaDataUpdated:
                    this.OnMetaDataUpdated(signal.State as MetaDataUpdatedSignalState);
                    break;
                case CommonSignals.HierarchiesUpdated:
                    this.Reset();
                    break;
                case CommonSignals.ImagesUpdated:
                    Logger.Write(this, LogLevel.Debug, "Images were updated, resetting cache.");
                    this.Reset();
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual void OnMetaDataUpdated(MetaDataUpdatedSignalState state)
        {
            if (state != null && state.Names != null)
            {
                this.Reset(state.Names);
            }
            else
            {
                this.Reset(Enumerable.Empty<string>());
            }
        }

        public Wrapper<ImageBrush> Create(LibraryHierarchyNode libraryHierarchyNode, LibraryBrowserTile libraryBrowserTile)
        {
            var placeholder = this.PlaceholderBrushFactory.Create(libraryBrowserTile.Width, libraryBrowserTile.Height);
            if (libraryHierarchyNode == null)
            {
                return AsyncResult<ImageBrush>.FromValue(placeholder);
            }
            var cache = string.IsNullOrEmpty(this.LibraryHierarchyBrowser.Filter);
            var factory = new Func<ImageBrush>(() =>
            {
                //TODO: Bad .Result
                var metaDataItems = new Func<MetaDataItem[]>(
                    () => LibraryHierarchyNodeConverter.Instance.Convert(libraryHierarchyNode).Result.MetaDatas.ToArray()
                );
                return this.Create(libraryHierarchyNode, metaDataItems, libraryBrowserTile.Width, libraryBrowserTile.Height, libraryBrowserTile.Mode, cache);
            });
            if (cache)
            {
                var value = new Func<Task<ImageBrush>>(() => this.Factory.StartNew(
                    () => this.Store.GetOrAdd(new Tuple<LibraryHierarchyNode, LibraryBrowserImageMode>(libraryHierarchyNode, libraryBrowserTile.Mode), libraryBrowserTile.Width, libraryBrowserTile.Height, false, factory)
                ));
                var brush = default(ImageBrush);
                if (this.Store.TryGetValue(new Tuple<LibraryHierarchyNode, LibraryBrowserImageMode>(libraryHierarchyNode, libraryBrowserTile.Mode), libraryBrowserTile.Width, libraryBrowserTile.Height, false, out brush))
                {
                    return new MonitoringAsyncResult<ImageBrush>(libraryHierarchyNode, brush, value);
                }
                return new MonitoringAsyncResult<ImageBrush>(libraryHierarchyNode, placeholder, value);
            }
            return new MonitoringAsyncResult<ImageBrush>(libraryHierarchyNode, placeholder, () => this.Factory.StartNew(factory));
        }

        protected virtual ImageBrush Create(LibraryHierarchyNode libraryHierarchyNode, Func<MetaDataItem[]> metaDataItems, int width, int height, LibraryBrowserImageMode mode, bool cache)
        {
            Logger.Write(this, LogLevel.Debug, "Creating brush: {0}x{1}", width, height);
            var source = this.LibraryBrowserTileProvider.CreateImageSource(
                libraryHierarchyNode,
                metaDataItems,
                width,
                height,
                mode,
                cache
            );
            if (source == null)
            {
                return this.PlaceholderBrushFactory.Create(width, height);
            }
            var brush = new ImageBrush(source)
            {
                Stretch = Stretch.Uniform
            };
            if (brush.CanFreeze)
            {
                brush.Freeze();
            }
            return brush;
        }

        protected virtual void CreateTaskFactory(int threads)
        {
            Logger.Write(this, LogLevel.Debug, "Creating task factory for {0} threads.", threads);
            this.Factory = new TaskFactory(new TaskScheduler(new ParallelOptions()
            {
                MaxDegreeOfParallelism = threads
            }));
        }

        protected virtual void CreateCache(int capacity)
        {
            Logger.Write(this, LogLevel.Debug, "Creating cache for {0} items.", capacity);
            this.Store = new ImageBrushCache<Tuple<LibraryHierarchyNode, LibraryBrowserImageMode>>(capacity);
        }

        protected virtual void Reset(IEnumerable<string> names)
        {
            if (names != null && names.Any())
            {
                if (!names.Contains(CommonImageTypes.FrontCover, StringComparer.OrdinalIgnoreCase))
                {
                    return;
                }
            }
            Logger.Write(this, LogLevel.Debug, "Meta data was updated, resetting cache.");
            this.Reset();
        }

        protected virtual void Reset()
        {
            this.Store.Clear();
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
        }

        ~LibraryBrowserTileBrushFactory()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        public class LibraryBrowserTile
        {
            public static readonly PixelSizeConverter Converter = ComponentRegistry.Instance.GetComponent<PixelSizeConverter>();

            public int Width { get; private set; }

            public int Height { get; private set; }

            public LibraryBrowserImageMode Mode { get; private set; }

            public bool IsEmpty
            {
                get
                {
                    return this.Width == 0 || this.Height == 0 || this.Mode == LibraryBrowserImageMode.None;
                }
            }

            public void Update(double width, double height, LibraryBrowserImageMode mode)
            {
                var size = Converter.Convert(new Size(width, height));
                this.Width = Convert.ToInt32(width);
                this.Height = Convert.ToInt32(height);
                this.Mode = mode;
            }
        }
    }
}
