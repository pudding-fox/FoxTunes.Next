using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    //Setting PRIORITY_HIGH so the the cache is cleared before being re-queried.
    [ComponentPriority(ComponentPriorityAttribute.HIGH)]
    public class MetaDataProviderCache : StandardComponent, IMetaDataProviderCache, IDisposable
    {
        public Lazy<MetaDataProvider[]> Providers { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public MetaDataProviderCache()
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
                case CommonSignals.MetaDataProvidersUpdated:
                    var providers = signal.State as IEnumerable<MetaDataProvider>;
                    if (providers != null && providers.Any())
                    {
                        //Nothing to do for indivudual column change.
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Debug, "Providers were updated, resetting cache.");
                        this.Providers = null;
                    }
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public MetaDataProvider[] GetProviders(Func<IEnumerable<MetaDataProvider>> factory)
        {
            if (this.Providers == null)
            {
                this.Providers = new Lazy<MetaDataProvider[]>(() => factory().ToArray());
            }
            return this.Providers.Value;
        }

        public void Reset()
        {
            this.Providers = null;
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

        ~MetaDataProviderCache()
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
