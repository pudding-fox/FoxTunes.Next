using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class WriteFileMetaDataTask : BackgroundTask
    {
        public const string ID = "76E69940-BF52-47F9-8A42-05738FD87F11";

        public WriteFileMetaDataTask(string fileName, IEnumerable<MetaDataItem> metaDataItems)
            : base(ID)
        {
            this.FileName = fileName;
            this.MetaDataItems = metaDataItems;
        }

        public string FileName { get; private set; }

        public IEnumerable<MetaDataItem> MetaDataItems { get; private set; }

        public IMetaDataSourceFactory MetaDataSourceFactory { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.MetaDataSourceFactory = core.Factories.MetaDataSource;
            base.InitializeComponent(core);
        }

        protected override Task OnRun()
        {
            var metaDataSource = this.MetaDataSourceFactory.Create();
            return metaDataSource.SetMetaData(
                this.FileName,
                this.MetaDataItems,
                metaDataItem => true
            );
        }
    }
}
