using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class ArtistImagePersistenceBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        public ISignalEmitter SignalEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                ArtistImagePersistenceBehaviourConfiguration.SECTION,
                ArtistImagePersistenceBehaviourConfiguration.ENABLED
            );
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            if (this.Enabled.Value)
            {
                switch (signal.Name)
                {
                    case CommonSignals.MetaDataUpdated:
                        if (signal.State is MetaDataUpdatedSignalState state && state.Names.Contains(CommonImageTypes.Artist, StringComparer.OrdinalIgnoreCase) && state.FileDatas != null && state.FileDatas.Any())
                        {
                            foreach (var fileData in state.FileDatas)
                            {
                                this.Save(fileData);
                            }
                        }
                        break;
                }
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual void Save(IFileData fileData)
        {
            try
            {
                lock (fileData.MetaDatas)
                {
                    var metaDataItem = fileData.MetaDatas.FirstOrDefault(_metaDataItem => string.Equals(_metaDataItem.Name, CommonImageTypes.Artist, StringComparison.OrdinalIgnoreCase) && _metaDataItem.Type == MetaDataItemType.Image);
                    if (metaDataItem != null && !string.IsNullOrEmpty(metaDataItem.Value))
                    {
                        Logger.Write(this, LogLevel.Info, "Saving artist image: {0}", metaDataItem.Value);
                        this.Save(fileData, metaDataItem.Value);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to save artist image: {0}", e.Message);
            }
        }

        protected virtual void Save(IFileData fileData, string sourceFileName)
        {
            var destinationFileName = Path.Combine(Path.GetDirectoryName(fileData.FileName), "Artist.bin");
            if (!File.Exists(destinationFileName))
            {
                File.Copy(sourceFileName, destinationFileName);
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return ArtistImagePersistenceBehaviourConfiguration.GetConfigurationSections();
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

        ~ArtistImagePersistenceBehaviour()
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
