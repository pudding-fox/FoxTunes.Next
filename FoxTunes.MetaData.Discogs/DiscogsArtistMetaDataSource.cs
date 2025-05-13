using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class DiscogsArtistMetaDataSource : StandardComponent, IOnDemandMetaDataSource
    {
        public DiscogsBehaviour Behaviour { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Behaviour = ComponentRegistry.Instance.GetComponent<DiscogsBehaviour>();
            base.InitializeComponent(core);
        }

        public bool Enabled
        {
            get
            {
                return this.Behaviour.Enabled.Value && this.Behaviour.AutoLookup.Value;
            }
        }

        public string Name
        {
            get
            {
                return CommonImageTypes.Artist;
            }
        }

        public MetaDataItemType Type
        {
            get
            {
                return MetaDataItemType.Image;
            }
        }

        public bool CanGetValue(IFileData fileData, OnDemandMetaDataRequest request)
        {
            if (request.UpdateType.HasFlag(MetaDataUpdateType.User))
            {
                //User requests are always processed.
                return true;
            }
            lock (fileData.MetaDatas)
            {
                var metaDataItem = fileData.MetaDatas.FirstOrDefault(
                    element => string.Equals(element.Name, CustomMetaData.DiscogsRelease, StringComparison.OrdinalIgnoreCase) && element.Type == MetaDataItemType.Tag
                );
                if (metaDataItem != null && string.Equals(metaDataItem.Value, Discogs.Release.None, StringComparison.OrdinalIgnoreCase))
                {
                    //We have previously attempted a lookup and it failed, don't try again (automatically).
                    return false;
                }
            }
            return true;
        }

        public async Task<OnDemandMetaDataValues> GetValues(IEnumerable<IFileData> fileDatas, OnDemandMetaDataRequest request)
        {
            var releaseLookups = await this.Behaviour.FetchReleases(fileDatas, request.UpdateType).ConfigureAwait(false);
            return this.Behaviour.GetMetaDataValues(releaseLookups, CommonImageTypes.Artist, false);
        }
    }
}
