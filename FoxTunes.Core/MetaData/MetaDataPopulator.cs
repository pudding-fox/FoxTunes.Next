using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FoxTunes
{
    public abstract class MetaDataPopulator : PopulatorBase
    {
        public const string ID = "EA40EA65-6F49-48A6-9469-DD5FC2E36EC0";

        protected MetaDataPopulator(IDatabaseComponent database, IDatabaseQuery query, bool reportProgress, ITransactionSource transaction)
            : base(reportProgress)
        {
            this.Database = database;
            this.Transaction = transaction;
            this.Query = query;
            this.Writer = new MetaDataWriter(this.Database, this.Query, this.Transaction);
            this.Warnings = new ConcurrentDictionary<IFileData, IList<string>>();
        }

        public IDatabaseComponent Database { get; private set; }

        public ITransactionSource Transaction { get; private set; }

        public IDatabaseQuery Query { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public IntegerConfigurationElement Threads { get; private set; }

        public IMetaDataSourceFactory MetaDataSourceFactory { get; private set; }

        private MetaDataWriter Writer { get; set; }

        public ConcurrentDictionary<IFileData, IList<string>> Warnings { get; private set; }

        public override ParallelOptions ParallelOptions
        {
            get
            {
                return new ParallelOptions()
                {
                    MaxDegreeOfParallelism = this.Threads.Value
                };
            }
        }

        public IFileData Current { get; private set; }

        private volatile int position = 0;

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Threads = this.Configuration.GetElement<IntegerConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.THREADS_ELEMENT
            );
            this.MetaDataSourceFactory = core.Factories.MetaDataSource;
            base.InitializeComponent(core);
        }

        public async Task Populate<T>(IEnumerable<T> fileDatas, CancellationToken cancellationToken) where T : IFileData
        {
            Logger.Write(this, LogLevel.Debug, "Begin populating meta data.");

            if (this.ReportProgress)
            {
                this.Name = "Populating meta data";
                this.Position = 0;
                this.Count = fileDatas.Count();
                if (this.Count <= 100)
                {
                    this.Timer.Interval = FAST_INTERVAL;
                }
                else if (this.Count < 1000)
                {
                    this.Timer.Interval = NORMAL_INTERVAL;
                }
                else
                {
                    this.Timer.Interval = LONG_INTERVAL;
                }
                this.Timer.Start();
            }

            var metaDataSource = this.MetaDataSourceFactory.Create();

            await AsyncParallel.ForEach(fileDatas, async fileData =>
            {
                Logger.Write(this, LogLevel.Debug, "Reading meta data from file \"{0}\".", fileData.FileName);
                try
                {
                    var metaData = await metaDataSource.GetMetaData(fileData.FileName).ConfigureAwait(false);

                    foreach (var warning in metaDataSource.GetWarnings(fileData.FileName))
                    {
                        this.AddWarning(fileData, warning);
                    }

#if NET40
                    this.Semaphore.Wait();
#else
                    await this.Semaphore.WaitAsync().ConfigureAwait(false);
#endif

                    try
                    {
                        foreach (var metaDataItem in metaData)
                        {
                            try
                            {
                                await this.Writer.Write(fileData.Id, metaDataItem).ConfigureAwait(false);
                            }
                            catch (Exception e)
                            {
                                Logger.Write(this, LogLevel.Debug, "Failed to write meta data entry from file \"{0}\" with name \"{1}\": {2}", fileData.FileName, metaDataItem.Name, e.Message);
                                this.AddWarning(fileData, e.Message);
                            }
                        }
                    }
                    finally
                    {
                        this.Semaphore.Release();
                    }

                    if (this.ReportProgress)
                    {
                        this.Current = fileData;
                        Interlocked.Increment(ref this.position);
                    }
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Debug, "Failed to read meta data from file \"{0}\": {1}", fileData.FileName, e.Message);
                    this.AddWarning(fileData, e.Message);
                }
            }, cancellationToken, this.ParallelOptions).ConfigureAwait(false);
        }

        protected override void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var count = this.position - this.Position;
                if (count != 0)
                {
                    lock (SyncRoot)
                    {
                        switch (this.Timer.Interval)
                        {
                            case NORMAL_INTERVAL:
                                count *= 2;
                                break;
                            case FAST_INTERVAL:
                                count *= 10;
                                break;
                        }
                    }
                    var eta = this.GetEta(count);
                    this.Name = string.Format("Populating meta data: {0} remaining @ {1} items/s", eta, count);
                    if (this.Current != null)
                    {
                        this.Description = Path.GetFileName(this.Current.FileName);
                    }
                    this.Position = this.position;
                }
                base.OnElapsed(sender, e);
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }

        protected virtual void AddWarning<T>(T fileData, string warning) where T : IFileData
        {
            this.Warnings.GetOrAdd(fileData, key => new List<string>()).Add(warning);
        }

        protected override void OnDisposing()
        {
            this.Semaphore.Dispose();
            this.Writer.Dispose();
            base.OnDisposing();
        }
    }
}
