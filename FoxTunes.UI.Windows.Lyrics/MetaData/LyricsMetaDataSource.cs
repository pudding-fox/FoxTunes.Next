using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class LyricsMetaDataSource : StandardComponent, IOnDemandMetaDataSource
    {
        public LyricsBehaviour Behaviour { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement AutoLookup { get; private set; }

        public SelectionConfigurationElement AutoLookupProvider { get; private set; }

        public override void InitializeComponent(Interfaces.ICore core)
        {
            this.Behaviour = ComponentRegistry.Instance.GetComponent<LyricsBehaviour>();
            this.Configuration = core.Components.Configuration;
            this.AutoLookup = this.Configuration.GetElement<BooleanConfigurationElement>(
                LyricsBehaviourConfiguration.SECTION,
                LyricsBehaviourConfiguration.AUTO_LOOKUP
            );
            this.AutoLookupProvider = this.Configuration.GetElement<SelectionConfigurationElement>(
                LyricsBehaviourConfiguration.SECTION,
                LyricsBehaviourConfiguration.AUTO_LOOKUP_PROVIDER
            );
            base.InitializeComponent(core);
        }

        public bool Enabled
        {
            get
            {
                return this.Behaviour.Enabled.Value && this.AutoLookup.Value;
            }
        }

        public string Name
        {
            get
            {
                return CommonMetaData.Lyrics;
            }
        }

        public MetaDataItemType Type
        {
            get
            {
                return MetaDataItemType.Tag;
            }
        }

        public bool CanGetValue(IFileData fileData, OnDemandMetaDataRequest request)
        {
            var provider = request.State as ILyricsProvider ?? this.GetAutoLookupProvider();
            if (provider == null)
            {
                return false;
            }
            if (request.UpdateType.HasFlag(MetaDataUpdateType.User))
            {
                //User requests are always processed.
                return true;
            }
            lock (fileData.MetaDatas)
            {
                var metaDataItem = fileData.MetaDatas.FirstOrDefault(
                    element => string.Equals(element.Name, CustomMetaData.LyricsRelease, StringComparison.OrdinalIgnoreCase) && element.Type == MetaDataItemType.Tag
                );
                if (metaDataItem != null && string.Equals(metaDataItem.Value, provider.None, StringComparison.OrdinalIgnoreCase))
                {
                    //We have previously attempted a lookup and it failed, don't try again (automatically).
                    return false;
                }
            }
            return true;
        }

        public async Task<OnDemandMetaDataValues> GetValues(IEnumerable<IFileData> fileDatas, OnDemandMetaDataRequest request)
        {
            var provider = request.State as ILyricsProvider ?? this.GetAutoLookupProvider();
            if (provider == null)
            {
                return null;
            }
            var values = new List<OnDemandMetaDataValue>();
            foreach (var fileData in fileDatas)
            {
                Logger.Write(this, LogLevel.Debug, "Looking up lyrics for file \"{0}\"..", fileData.FileName);
                var result = await provider.Lookup(fileData).ConfigureAwait(false);
                if (!result.Success)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to look up lyrics for file \"{0}\".", fileData.FileName);
                    continue;
                }
                Logger.Write(this, LogLevel.Debug, "Looking up lyrics for file \"{0}\": OK.", fileData.FileName);
                values.Add(new OnDemandMetaDataValue(fileData, result.Lyrics));
            }
            var flags = MetaDataUpdateFlags.None;
            if (this.Behaviour.WriteTags.Value)
            {
                flags |= MetaDataUpdateFlags.WriteToFiles;
            }
            return new OnDemandMetaDataValues(values, flags);
        }

        protected virtual ILyricsProvider GetAutoLookupProvider()
        {
            var provider = default(ILyricsProvider);
            if (this.AutoLookupProvider.Value != null)
            {
                provider = this.Behaviour.Providers.FirstOrDefault(
                   _provider => string.Equals(_provider.Id, this.AutoLookupProvider.Value.Id, StringComparison.OrdinalIgnoreCase)
               );
            }
            if (provider == null)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to determine the preferred provider.");
                provider = this.Behaviour.Providers.FirstOrDefault();
            }
            if (provider == null)
            {
                Logger.Write(this, LogLevel.Warn, "No providers.");
            }
            return provider;
        }
    }
}
