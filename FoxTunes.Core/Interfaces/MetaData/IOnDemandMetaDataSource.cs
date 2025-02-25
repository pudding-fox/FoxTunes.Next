using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IOnDemandMetaDataSource
    {
        bool Enabled { get; }

        string Name { get; }

        MetaDataItemType Type { get; }

        bool CanGetValue(IFileData fileData, OnDemandMetaDataRequest request);

        Task<OnDemandMetaDataValues> GetValues(IEnumerable<IFileData> fileDatas, OnDemandMetaDataRequest request);
    }

    public class OnDemandMetaDataRequest
    {
        public OnDemandMetaDataRequest(string name, MetaDataItemType itemType, MetaDataUpdateType updateType, object state = null)
        {
            this.Name = name;
            this.ItemType = itemType;
            this.UpdateType = updateType;
            this.State = state;
        }

        public string Name { get; private set; }

        public MetaDataItemType ItemType { get; private set; }

        public MetaDataUpdateType UpdateType { get; private set; }

        public object State { get; private set; }
    }

    public class OnDemandMetaDataValues
    {
        public OnDemandMetaDataValues(IEnumerable<OnDemandMetaDataValue> values, MetaDataUpdateFlags flags)
        {
            this.Values = values;
            this.Flags = flags;
        }

        public IEnumerable<OnDemandMetaDataValue> Values { get; private set; }

        public MetaDataUpdateFlags Flags { get; private set; }
    }

    public class OnDemandMetaDataValue
    {
        public OnDemandMetaDataValue(IFileData fileData, string value)
        {
            this.FileData = fileData;
            this.Value = value;
        }

        public IFileData FileData { get; private set; }

        public string Value { get; private set; }
    }
}
