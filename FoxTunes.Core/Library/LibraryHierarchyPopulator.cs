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
    public class LibraryHierarchyPopulator : PopulatorBase
    {
        public LibraryHierarchyPopulator(IDatabaseComponent database, bool reportProgress, ITransactionSource transaction)
            : base(reportProgress)
        {
            this.Database = database;
            this.Transaction = transaction;
#if NET40
            this.Contexts = new TrackingThreadLocal<IScriptingContext>();
#else
            this.Contexts = new ThreadLocal<IScriptingContext>(true);
#endif
            this.Writer = new LibraryHierarchyWriter(this.Database, this.Transaction);
            this.Roots = new Lazy<IEnumerable<string>>(() => this.GetRoots());
        }

        public IDatabaseComponent Database { get; private set; }

        public ITransactionSource Transaction { get; private set; }

        public IScriptingRuntime ScriptingRuntime { get; private set; }

#if NET40
        private TrackingThreadLocal<IScriptingContext> Contexts { get; set; }
#else
        private ThreadLocal<IScriptingContext> Contexts { get; set; }
#endif

        private LibraryHierarchyWriter Writer { get; set; }

        public Lazy<IEnumerable<string>> Roots { get; private set; }

        public string Current { get; private set; }

        private volatile int position = 0;

        public override void InitializeComponent(ICore core)
        {
            this.ScriptingRuntime = core.Components.ScriptingRuntime;
            base.InitializeComponent(core);
        }

        public async Task Populate(LibraryItemStatus? status, CancellationToken cancellationToken)
        {
            var libraryHierarchies = this.GetHierarchies(this.Transaction);

            if (libraryHierarchies.Length == 0)
            {
                Logger.Write(this, LogLevel.Warn, "No library hierarchies are defined (or enabled).");
                return;
            }

            var libraryHierarchyLevels = this.GetLevels(libraryHierarchies);
            var libraryItems = this.GetItems(status, this.Transaction);

            if (this.ReportProgress)
            {
                this.Name = "Populating library hierarchies";
                this.Position = 0;
                this.Count = libraryItems.Count();
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

            await AsyncParallel.ForEach(libraryItems, async libraryItem =>
            {
                foreach (var libraryHierarchy in libraryHierarchies)
                {
                    await this.Populate(libraryItem, libraryHierarchy, libraryHierarchyLevels[libraryHierarchy]).ConfigureAwait(false);
                }

                if (this.ReportProgress)
                {
                    this.Current = libraryItem.FileName;
                    Interlocked.Increment(ref this.position);
                }
            }, cancellationToken, this.ParallelOptions).ConfigureAwait(false);
        }

        private async Task Populate(LibraryItem libraryItem, LibraryHierarchy libraryHierarchy, LibraryHierarchyLevel[] libraryHierarchyLevels)
        {
            var parentId = default(int?);
            switch (libraryHierarchy.Type)
            {
                case LibraryHierarchyType.Script:
                    for (int a = 0, b = libraryHierarchyLevels.Length - 1; a <= b; a++)
                    {
                        parentId = await this.Populate(libraryItem, libraryHierarchy, libraryHierarchyLevels[a], parentId, a == b);
                    }
                    break;
                case LibraryHierarchyType.FileSystem:
                    var pathSegments = this.GetPathSegments(libraryItem.FileName);
                    for (int a = 0, b = pathSegments.Length - 1; a <= b; a++)
                    {
                        parentId = await this.Populate(libraryItem, libraryHierarchy, pathSegments[a], parentId, a == b);
                    }
                    break;
            }
        }

        private Task<int> Populate(LibraryItem libraryItem, LibraryHierarchy libraryHierarchy, LibraryHierarchyLevel libraryHierarchyLevel, int? parentId, bool isLeaf)
        {
            var runner = new LibraryItemScriptRunner(
                this.GetOrAddContext(),
                libraryItem,
                libraryHierarchyLevel.Script
            );
            runner.Prepare();
            var value = Convert.ToString(runner.Run());
            return this.Populate(libraryItem, libraryHierarchy, value, parentId, isLeaf);
        }

        private async Task<int> Populate(LibraryItem libraryItem, LibraryHierarchy libraryHierarchy, string value, int? parentId, bool isLeaf)
        {
#if NET40
            this.Semaphore.Wait();
#else
            await this.Semaphore.WaitAsync().ConfigureAwait(false);
#endif
            try
            {
                return await this.Writer.Write(libraryHierarchy, libraryItem.Id, parentId, value, isLeaf).ConfigureAwait(false);
            }
            finally
            {
                this.Semaphore.Release();
            }
        }

        private LibraryHierarchy[] GetHierarchies(ITransactionSource transaction)
        {
            var queryable = this.Database.AsQueryable<LibraryHierarchy>(transaction);
            return queryable.Where(libraryHierarchy => libraryHierarchy.Enabled).ToArray();
        }

        private IDictionary<LibraryHierarchy, LibraryHierarchyLevel[]> GetLevels(IEnumerable<LibraryHierarchy> libraryHierarchies)
        {
            return libraryHierarchies.ToDictionary(
                libraryHierarchy => libraryHierarchy,
                libraryHierarchy => libraryHierarchy.Levels.OrderBy(libraryHierarchyLevel => libraryHierarchyLevel.Sequence).ToArray()
            );
        }

        private IQueryable<LibraryItem> GetItems(LibraryItemStatus? status, ITransactionSource transaction)
        {
            var queryable = this.Database.AsQueryable<LibraryItem>(transaction);
            if (!status.HasValue)
            {
                return queryable.Where(libraryItem => libraryItem.MetaDatas.Any());
            }
            else
            {
                return queryable.Where(libraryItem => libraryItem.Status == status.Value && libraryItem.MetaDatas.Any());
            }
        }

        private string[] GetPathSegments(string fileName)
        {
            //We can't really understand paths provided by plugins.
            //If it's a url we can organize by the scheme and absolute path.
            if (string.IsNullOrEmpty(Path.GetPathRoot(fileName)))
            {
                var url = default(Uri);
                if (Uri.TryCreate(fileName, UriKind.RelativeOrAbsolute, out url))
                {
                    fileName = Uri.UnescapeDataString(
                        url.AbsolutePath
                    ).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                    return new[]
                    {
                        Strings.LibraryHierarchyPopulator_Plugin,
                        url.Scheme.UCFirst(),
                        Path.GetFileName(fileName)
                    };
                }
                else
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to parse library item path: {0}", fileName);
                    return new string[] { };
                }
            }
            //This removes any matching library roots from the path.
            var normalized = fileName.Replace(this.Roots.Value, string.Empty, true, true);
            var segments = normalized.Split(
                new[] { Path.DirectorySeparatorChar.ToString() },
                StringSplitOptions.RemoveEmptyEntries
            ).ToArray();
            return segments;
        }

        private IScriptingContext GetOrAddContext()
        {
            if (this.Contexts.IsValueCreated)
            {
                return this.Contexts.Value;
            }
            return this.Contexts.Value = this.ScriptingRuntime.CreateContext();
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
                    this.Name = string.Format("Populating library hierarchies: {0} remaining @ {1} items/s", eta, count);
                    if (this.Current != null)
                    {
                        this.Description = Path.GetFileName(this.Current);
                    }
                    this.Position = position;
                }
                base.OnElapsed(sender, e);
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }

        protected IEnumerable<string> GetRoots()
        {
            var roots = this.Database.Set<LibraryRoot>(this.Transaction).Select(
                libraryRoot => libraryRoot.DirectoryName
            ).ToArray();
            //If a single folder has been added to the library then we create root nodes for each sub folder.
            //Otherwise we create a root node for each one.
            if (roots.Length > 1)
            {
                //We try to project the parent folder (of the root).
                //If it's empty then the drive letter will be used.
                roots = roots.Select(root =>
                {
                    var parent = Path.GetDirectoryName(root);
                    if (!string.IsNullOrEmpty(parent))
                    {
                        return parent;
                    }
                    return root;
                    //De-duplicate and sort. Sorting is for performance, it means a match can be made in less iterations (potentially).
                    //See GetPathSegments.
                }).Distinct().OrderBy(root => root).ToArray();
            }
            //TODO: We don't handle cases where a root exists within another.
            return roots;
        }

        protected override void OnDisposing()
        {
            if (this.Contexts != null)
            {
                foreach (var context in this.Contexts.Values)
                {
                    context.Dispose();
                }
                this.Contexts.Dispose();
            }
            if (this.Writer != null)
            {
                this.Writer.Dispose();
            }
            base.OnDisposing();
        }
    }
}
