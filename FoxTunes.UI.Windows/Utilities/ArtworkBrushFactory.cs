using FoxTunes.Interfaces;
using FoxTunes.ViewModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media;

namespace FoxTunes
{
    //Setting PRIORITY_HIGH so the the cache is cleared before being re-queried.
    [ComponentPriority(ComponentPriorityAttribute.HIGH)]
    [WindowsUserInterfaceDependency]
    //TODO: This was (and technically still is) a StandardFactory, but it overrides the ComponentPriorityAttribute.HIGH.
    //TODO: I hate the Factory, Manager, Behaviour and Component sub types and they should go away...
    public class ArtworkBrushFactory : StandardComponent, IDisposable
    {
        public PixelSizeConverter PixelSizeConverter { get; private set; }

        public ImageLoader ImageLoader { get; private set; }

        public ArtworkPlaceholderBrushFactory PlaceholderBrushFactory { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public ImageBrushCache<string> Store { get; private set; }

        public TaskFactory Factory { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PixelSizeConverter = ComponentRegistry.Instance.GetComponent<PixelSizeConverter>();
            this.ImageLoader = ComponentRegistry.Instance.GetComponent<ImageLoader>();
            this.PlaceholderBrushFactory = ComponentRegistry.Instance.GetComponent<ArtworkPlaceholderBrushFactory>();
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

        public AsyncResult<ImageBrush> Create(string fileName, int width, int height)
        {
            this.PixelSizeConverter.Convert(ref width, ref height);
            var placeholder = this.PlaceholderBrushFactory.Create(width, height);
            if (string.IsNullOrEmpty(fileName) || !File.Exists(fileName))
            {
                return AsyncResult<ImageBrush>.FromValue(placeholder);
            }
            var brush = default(ImageBrush);
            if (this.Store.TryGetValue(fileName, width, height, out brush))
            {
                if (brush != null)
                {
                    return AsyncResult<ImageBrush>.FromValue(brush);
                }
                else
                {
                    return AsyncResult<ImageBrush>.FromValue(placeholder);
                }
            }
            return new AsyncResult<ImageBrush>(placeholder, this.Factory.StartNew(() =>
            {
                return this.Store.GetOrAdd(fileName, width, height, () => this.Create(fileName, width, height, true));
            }));
        }

        protected virtual ImageBrush Create(string fileName, int width, int height, bool cache)
        {
            Logger.Write(this, LogLevel.Debug, "Creating brush: {0}x{1}", width, height);
            var source = this.ImageLoader.Load(
                fileName,
                width,
                height,
                cache
            );
            if (source == null)
            {
                return null;
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
            this.Store = new ImageBrushCache<string>(capacity);
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

        ~ArtworkBrushFactory()
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
    }
}
