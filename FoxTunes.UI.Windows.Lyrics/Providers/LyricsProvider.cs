using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class LyricsProvider : StandardComponent, ILyricsProvider
    {
        protected LyricsProvider(string id, string name = null, string description = null)
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public LyricsBehaviour Behaviour { get; private set; }

        public IMetaDataManager MetaDataManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Behaviour = ComponentRegistry.Instance.GetComponent<LyricsBehaviour>();
            this.MetaDataManager = core.Managers.MetaData;
            base.InitializeComponent(core);
        }

        public abstract string None { get; }

        public abstract Task<LyricsResult> Lookup(IFileData fileData);

        protected virtual async Task SaveMetaData(IFileData fileData, string releaseId)
        {
            lock (fileData.MetaDatas)
            {
                var metaDataItem = fileData.MetaDatas.FirstOrDefault(
                    element => string.Equals(element.Name, CustomMetaData.LyricsRelease, StringComparison.OrdinalIgnoreCase) && element.Type == MetaDataItemType.Tag
                );
                if (metaDataItem == null)
                {
                    metaDataItem = new MetaDataItem(CustomMetaData.LyricsRelease, MetaDataItemType.Tag);
                    fileData.MetaDatas.Add(metaDataItem);
                }
                metaDataItem.Value = releaseId;
            }
            await this.MetaDataManager.Save(
                new[] { fileData },
                new[] { CustomMetaData.LyricsRelease },
                MetaDataUpdateType.System,
                MetaDataUpdateFlags.None
            ).ConfigureAwait(false);
        }
    }
}
