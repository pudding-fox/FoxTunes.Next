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
            return true;
        }

        public async Task<OnDemandMetaDataValues> GetValues(IEnumerable<IFileData> fileDatas, OnDemandMetaDataRequest request)
        {
            var releaseLookups = await this.Behaviour.FetchReleases(fileDatas, request.UpdateType).ConfigureAwait(false);
            return this.Behaviour.GetMetaDataValues(releaseLookups, CommonImageTypes.Artist, false);
        }
    }
}
