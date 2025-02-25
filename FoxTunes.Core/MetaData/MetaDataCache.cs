using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    //Setting PRIORITY_HIGH so the the cache is cleared before being re-queried.
    [ComponentPriority(ComponentPriorityAttribute.HIGH)]
    public class MetaDataCache : StandardComponent, IMetaDataCache, IDisposable
    {
        public static readonly KeyLock<MetaDataCacheKey> KeyLock = new KeyLock<MetaDataCacheKey>();

        public IEnumerable<MetaDataCacheKey> Keys
        {
            get
            {
                return this.Values.Keys.ToArray();
            }
        }

        public ConcurrentDictionary<MetaDataCacheKey, Lazy<MetaDataItem[]>> Values { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public MetaDataCache()
        {
            this.Reset();
        }

        public override void InitializeComponent(ICore core)
        {
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
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
                    Logger.Write(this, LogLevel.Debug, "Hierarchies were updated, resetting cache.");
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
            if (state == null)
            {
                Logger.Write(this, LogLevel.Debug, "Meta data was updated, resetting cache.");
                this.Reset();
            }
            else
            {
                var keys = this.Keys;
                if (state.Names != null && state.Names.Any())
                {
                    keys = keys.Where(key => state.Names.Contains(key.Name, StringComparer.OrdinalIgnoreCase));
                }
                if (state.FileDatas != null && state.FileDatas.Any())
                {
                    var libraryHierarchyNodes = state.FileDatas.GetParents();
                    var playlistItems = state.FileDatas.OfType<PlaylistItem>();
                    keys = keys.Where(key =>
                    {
                        if (key is LibraryMetaDataCacheKey libraryMetaDataCacheKey)
                        {
                            return libraryHierarchyNodes.Contains(libraryMetaDataCacheKey.LibraryHierarchyNode);
                        }
                        else if (key is PlaylistMetaDataCacheKey playlistMetaDataCacheKey)
                        {
                            return playlistItems.Intersect(playlistMetaDataCacheKey.PlaylistItems).Any();
                        }
                        return false;
                    });
                }
                foreach (var key in keys)
                {
                    this.Evict(key);
                }
            }
        }

        public MetaDataItem[] GetMetaDatas(MetaDataCacheKey key, Func<IEnumerable<MetaDataItem>> factory)
        {
            return this.Values.GetOrAdd(
                key,
                () => new Lazy<MetaDataItem[]>(() => factory().ToArray())
            ).Value;
        }

        public async Task<MetaDataItem[]> GetMetaDatas(MetaDataCacheKey key, Func<Task<IEnumerable<MetaDataItem>>> factory)
        {
            var value = default(Lazy<MetaDataItem[]>);
            if (this.Values.TryGetValue(key, out value))
            {
                return value.Value;
            }
            using (await KeyLock.LockAsync(key).ConfigureAwait(false))
            {
                if (this.Values.TryGetValue(key, out value))
                {
                    return value.Value;
                }
                var metaDataItems = await factory().ConfigureAwait(false);
                return this.Values.GetOrAdd(key, () => new Lazy<MetaDataItem[]>(() => metaDataItems.ToArray())).Value;
            }
        }

        public void Reset()
        {
            this.Values = new ConcurrentDictionary<MetaDataCacheKey, Lazy<MetaDataItem[]>>();
        }

        public void Evict(MetaDataCacheKey key)
        {
            Logger.Write(this, LogLevel.Debug, "Evicting cache entry: {0}", key);
            this.Values.TryRemove(key);
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

        ~MetaDataCache()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
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
