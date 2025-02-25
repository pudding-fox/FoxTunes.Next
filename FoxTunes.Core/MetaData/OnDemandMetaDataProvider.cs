using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class OnDemandMetaDataProvider : StandardComponent, IOnDemandMetaDataProvider
    {
        public static readonly KeyLock<string> KeyLock = new KeyLock<string>(StringComparer.OrdinalIgnoreCase);

        public OnDemandMetaDataProvider()
        {
            this.Sources = new List<IOnDemandMetaDataSource>();
        }

        public IList<IOnDemandMetaDataSource> Sources { get; private set; }

        public IMetaDataManager MetaDataManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Sources.AddRange(ComponentRegistry.Instance.GetComponents<IOnDemandMetaDataSource>());
            this.MetaDataManager = core.Managers.MetaData;
            base.InitializeComponent(core);
        }

        public bool IsSourceEnabled(string name, MetaDataItemType type)
        {
            return this.GetSources(name, type).Any();
        }

        public async Task<string> GetMetaData(IFileData fileData, OnDemandMetaDataRequest request)
        {
            var values = await this.GetMetaData(new[] { fileData }, request).ConfigureAwait(false);
            return values.FirstOrDefault();
        }

        public async Task<IEnumerable<string>> GetMetaData(IEnumerable<IFileData> fileDatas, OnDemandMetaDataRequest request)
        {
            using (await KeyLock.LockAsync(request.Name).ConfigureAwait(false))
            {
                var values = this.GetCurrentMetaData(fileDatas, request);
                var queue = new HashSet<IFileData>(fileDatas.Except(values.Keys));
                if (queue.Any())
                {
                    var sources = this.GetSources(request.Name, request.ItemType);
                    foreach (var source in sources)
                    {
                        var result = await source.GetValues(
                            queue.Where(fileData => source.CanGetValue(fileData, request)).ToArray(),
                            request
                        ).ConfigureAwait(false);
                        if (result != null && result.Values.Any())
                        {
                            foreach (var value in result.Values)
                            {
                                this.AddMetaData(request, value);
                                values[value.FileData] = value.Value;
                                queue.Remove(value.FileData);
                            }
                            this.Dispatch(() => this.SaveMetaData(request, result));
                        }
                    }
                }
                return new HashSet<string>(values.Values, StringComparer.OrdinalIgnoreCase);
            }
        }

        public string GetCurrentMetaData(IFileData fileData, OnDemandMetaDataRequest request)
        {
            var values = this.GetCurrentMetaData(new[] { fileData }, request);
            return values.Values.FirstOrDefault();
        }

        public IDictionary<IFileData, string> GetCurrentMetaData(IEnumerable<IFileData> fileDatas, OnDemandMetaDataRequest request)
        {
            var values = new Dictionary<IFileData, string>();
            foreach (var fileData in fileDatas)
            {
                lock (fileData.MetaDatas)
                {
                    var metaDataItem = fileData.MetaDatas.FirstOrDefault(
                         element => string.Equals(element.Name, request.Name, StringComparison.OrdinalIgnoreCase) && element.Type == request.ItemType
                    );
                    if (metaDataItem != null)
                    {
                        values[fileData] = metaDataItem.Value;
                    }
                }
            }
            return values;
        }

        public Task SetMetaData(OnDemandMetaDataRequest request, OnDemandMetaDataValues result)
        {
            foreach (var value in result.Values)
            {
                this.AddMetaData(request, value);
            }
            return this.SaveMetaData(request, result);
        }

        protected virtual IEnumerable<IOnDemandMetaDataSource> GetSources(string name, MetaDataItemType type)
        {
            foreach (var source in this.Sources)
            {
                if (source.Enabled && string.Equals(source.Name, name, StringComparison.OrdinalIgnoreCase) && source.Type == type)
                {
                    yield return source;
                }
            }
        }

        protected virtual void AddMetaData(OnDemandMetaDataRequest request, OnDemandMetaDataValue value)
        {
            lock (value.FileData.MetaDatas)
            {
                var metaDataItem = value.FileData.MetaDatas.FirstOrDefault(
                    element => string.Equals(element.Name, request.Name, StringComparison.OrdinalIgnoreCase) && element.Type == request.ItemType
                );
                if (metaDataItem == null)
                {
                    metaDataItem = new MetaDataItem(request.Name, request.ItemType);
                    value.FileData.MetaDatas.Add(metaDataItem);
                }
                metaDataItem.Value = value.Value;
            }
        }

        protected virtual Task SaveMetaData(OnDemandMetaDataRequest request, OnDemandMetaDataValues result)
        {
            var fileDatas = result.Values.Select(value => value.FileData).ToArray();
            return this.MetaDataManager.Save(fileDatas, new[] { request.Name }, request.UpdateType, result.Flags);
        }
    }
}