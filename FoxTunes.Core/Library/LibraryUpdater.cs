#pragma warning disable 612, 618
using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FoxTunes
{
    public class LibraryUpdater : PopulatorBase
    {
        public LibraryUpdater(IDatabaseComponent database, IEnumerable<LibraryItem> items, Func<LibraryItem, bool> predicate, Func<IDatabaseSet<LibraryItem>, LibraryItem, Task> task, bool reportProgress, ITransactionSource transaction)
            : base(reportProgress)
        {
            this.Database = database;
            this.Items = items;
            this.Predicate = predicate;
            this.Task = task;
            this.Transaction = transaction;
        }

        public IDatabaseComponent Database { get; private set; }

        public IEnumerable<LibraryItem> Items { get; private set; }

        public Func<LibraryItem, bool> Predicate { get; private set; }

        public Func<IDatabaseSet<LibraryItem>, LibraryItem, Task> Task { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public ITransactionSource Transaction { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public IntegerConfigurationElement Threads { get; private set; }

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
            base.InitializeComponent(core);
        }

        public async Task Populate(CancellationToken cancellationToken)
        {
            //Using source without relations.
            //TODO: Add this kind of thing to FoxDb, we're actually performing some schema queries to create this structure.
            var source = this.Database.Source(
                this.Database.Config.Transient.Table<LibraryItem>(TableFlags.AutoColumns),
                this.Transaction
            );
            var set = this.Database.Set<LibraryItem>(source);

            if (this.ReportProgress)
            {
                this.Name = "Updating library";
                this.Position = 0;
                if (this.Items != null && this.Items.Any())
                {
                    this.Count = this.Items.Count();
                }
                else
                {
                    this.Count = set.Count;
                }
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

            var sequence = default(IEnumerable<LibraryItem>);
            if (this.Items != null && this.Items.Any())
            {
                sequence = this.Items;
            }
            else
            {
                sequence = set;
            }
            await AsyncParallel.ForEach(sequence, async libraryItem =>
            {
                if (this.Predicate(libraryItem))
                {
                    await this.Task(set, libraryItem).ConfigureAwait(false);
                }

                if (this.ReportProgress)
                {
                    this.Current = libraryItem;
                    Interlocked.Increment(ref this.position);
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
                        if (this.Timer == null)
                        {
                            return;
                        }
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
                    this.Name = string.Format("Updating library: {0} remaining @ {1} items/s", eta, count);
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
    }
}
