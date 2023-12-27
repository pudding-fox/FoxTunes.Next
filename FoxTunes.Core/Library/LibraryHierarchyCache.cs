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
    public class LibraryHierarchyCache : StandardComponent, ILibraryHierarchyCache, IDisposable
    {
        public bool HasItems
        {
            get
            {
                foreach (var key in this.Keys)
                {
                    var value = default(Lazy<LibraryHierarchyNode[]>);
                    if (!this.Nodes.TryGetValue(key, out value))
                    {
                        continue;
                    }
                    if (!value.IsValueCreated)
                    {
                        continue;
                    }
                    if (value.Value.Length > 0)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public IEnumerable<LibraryHierarchyCacheKey> Keys
        {
            get
            {
                return this.Nodes.Keys.Concat(this.Items.Keys).Distinct().ToArray();
            }
        }

        public Lazy<LibraryHierarchy[]> Hierarchies { get; private set; }

        public ConcurrentDictionary<LibraryHierarchyCacheKey, Lazy<LibraryHierarchyNode[]>> Nodes { get; private set; }

        public ConcurrentDictionary<LibraryHierarchyCacheKey, Lazy<LibraryItem[]>> Items { get; private set; }

        public IFilterParser FilterParser { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public LibraryHierarchyCache()
        {
            this.Reset();
        }

        public override void InitializeComponent(ICore core)
        {
            this.FilterParser = core.Components.FilterParser;
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
            var appliesTo = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in this.Keys)
            {
                if (string.IsNullOrEmpty(key.Filter))
                {
                    //No filter.
                    continue;
                }
                if (state != null && state.Names != null && state.Names.Any())
                {
                    //If we know what meta data was updated then check if the filter applies to it.
                    if (!appliesTo.GetOrAdd(key.Filter, filter => this.FilterParser.AppliesTo(filter, state.Names)))
                    {
                        //It's unrelated.
                        continue;
                    }
                }
                //The cache entry is affected by the meta data update, invalidate it.
                this.Evict(key);
            }
        }

        public LibraryHierarchy[] GetHierarchies(Func<IEnumerable<LibraryHierarchy>> factory)
        {
            if (this.Hierarchies == null)
            {
                this.Hierarchies = new Lazy<LibraryHierarchy[]>(() => factory().ToArray());
            }
            return this.Hierarchies.Value;
        }

        public LibraryHierarchyNode[] GetNodes(LibraryHierarchyCacheKey key, Func<IEnumerable<LibraryHierarchyNode>> factory)
        {
            return this.Nodes.GetOrAdd(key, () => new Lazy<LibraryHierarchyNode[]>(() => factory().ToArray())).Value;
        }

        public LibraryItem[] GetItems(LibraryHierarchyCacheKey key, Func<IEnumerable<LibraryItem>> factory)
        {
            return this.Items.GetOrAdd(key, () => new Lazy<LibraryItem[]>(() => factory().ToArray())).Value;
        }

        public void Reset()
        {
            this.Hierarchies = null;
            this.Nodes = new ConcurrentDictionary<LibraryHierarchyCacheKey, Lazy<LibraryHierarchyNode[]>>();
            this.Items = new ConcurrentDictionary<LibraryHierarchyCacheKey, Lazy<LibraryItem[]>>();
        }

        public void Evict(LibraryHierarchyCacheKey key)
        {
            Logger.Write(this, LogLevel.Debug, "Evicting cache entry: {0}", key);
            this.Nodes.TryRemove(key);
            this.Items.TryRemove(key);
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

        ~LibraryHierarchyCache()
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
