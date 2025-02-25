using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class SynchronizeLibraryMetaDataTask : BackgroundTask
    {
        public const string ID = "82DE2133-2930-45E2-BDA6-F2B426306203";

        public SynchronizeLibraryMetaDataTask(IEnumerable<LibraryItem> libraryItems, IEnumerable<string> names) : base(ID)
        {
            this.LibraryItems = libraryItems;
            this.Names = names;
            this.Errors = new Dictionary<LibraryItem, IList<string>>();
        }

        public IEnumerable<LibraryItem> LibraryItems { get; private set; }

        public IEnumerable<string> Names { get; private set; }

        public IDictionary<LibraryItem, IList<string>> Errors { get; private set; }

        public IOutput Output { get; private set; }

        public IDatabaseComponent Database { get; private set; }

        public IMetaDataSourceFactory MetaDataSourceFactory { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public SelectionConfigurationElement Write { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output;
            this.Database = core.Factories.Database.Create();
            this.MetaDataSourceFactory = core.Factories.MetaDataSource;
            this.Configuration = core.Components.Configuration;
            this.Write = this.Configuration.GetElement<SelectionConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.WRITE_ELEMENT
            );
            base.InitializeComponent(core);
        }

        protected override async Task OnRun()
        {
            if (MetaDataBehaviourConfiguration.GetWriteBehaviour(this.Write.Value) == WriteBehaviour.None)
            {
                Logger.Write(this, LogLevel.Debug, "Meta data writing is disabled.");
                return;
            }
            foreach (var libraryItem in this.LibraryItems)
            {
                if (this.Output.IsLoaded(libraryItem.FileName))
                {
                    Logger.Write(this, LogLevel.Debug, "File \"{0}\" could not be written, the update will be retried: The file is in use.", libraryItem.FileName);
                    this.AddError(libraryItem, string.Format("File \"{0}\" could not be written, the update will be retried: The file is in use.", libraryItem.FileName));
                    await this.Schedule(libraryItem).ConfigureAwait(false);
                    continue;
                }
                try
                {
                    await this.Synchronize(libraryItem).ConfigureAwait(false);
                }
                catch (IOException e)
                {
                    Logger.Write(this, LogLevel.Debug, "File \"{0}\" could not be written, the update will be retried: {1}", libraryItem.FileName, e.Message);
                    this.AddError(libraryItem, string.Format("File \"{0}\" could not be written, the update will be retried: {1}", libraryItem.FileName, e.Message));
                    await this.Schedule(libraryItem).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Debug, "File \"{0}\" could not be written: {1}", libraryItem.FileName, e.Message);
                    this.AddError(libraryItem, string.Format("File \"{0}\" could not be written: {1}", libraryItem.FileName, e.Message));
                    await this.Deschedule(libraryItem).ConfigureAwait(false);
                }
            }
        }

        protected virtual async Task Synchronize(LibraryItem libraryItem)
        {
            if (!FileSystemHelper.IsLocalPath(libraryItem.FileName))
            {
                Logger.Write(this, LogLevel.Debug, "File \"{0}\" is not a local file: Cannot update.", libraryItem.FileName);
                return;
            }

            if (!File.Exists(libraryItem.FileName))
            {
                Logger.Write(this, LogLevel.Debug, "File \"{0}\" no longer exists: Cannot update.", libraryItem.FileName);
                return;
            }

            var metaDataSource = this.MetaDataSourceFactory.Create();
            await metaDataSource.SetMetaData(
                libraryItem.FileName,
                libraryItem.MetaDatas,
                metaDataItem => this.Names == null || !this.Names.Any() || this.Names.Contains(metaDataItem.Name, StringComparer.OrdinalIgnoreCase)
            ).ConfigureAwait(false);

            await this.Deschedule(libraryItem).ConfigureAwait(false);
        }

        protected virtual Task Schedule(LibraryItem libraryItem)
        {
            libraryItem.Flags |= LibraryItemFlags.Export;
            return LibraryTaskBase.UpdateLibraryItem(this.Database, libraryItem);
        }

        protected virtual Task Deschedule(LibraryItem libraryItem)
        {
            libraryItem.ImportDate = DateTimeHelper.ToString(DateTime.UtcNow.AddSeconds(30));
            libraryItem.Flags &= ~LibraryItemFlags.Export;
            return LibraryTaskBase.UpdateLibraryItem(this.Database, libraryItem);
        }

        protected virtual void AddError(LibraryItem libraryItem, string message)
        {
            var errors = default(IList<string>);
            if (!this.Errors.TryGetValue(libraryItem, out errors))
            {
                errors = new List<string>();
                this.Errors.Add(libraryItem, errors);
            }
            errors.Add(message);
        }

        protected override void OnDisposing()
        {
            if (this.Database != null)
            {
                this.Database.Dispose();
            }
            base.OnDisposing();
        }
    }
}
