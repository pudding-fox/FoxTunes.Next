using FoxDb;
using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class HierarchyManager : StandardManager, IHierarchyManager
    {
        public HierarchyManagerState State
        {
            get
            {
                if (global::FoxTunes.BackgroundTask.Active.Any(backgroundTask =>
                {
                    var type = backgroundTask.GetType();
                    return type == typeof(BuildLibraryHierarchiesTask) || type == typeof(ClearLibraryHierarchiesTask);
                }))
                {
                    return HierarchyManagerState.Updating;
                }
                return HierarchyManagerState.None;
            }
        }

        public ICore Core { get; private set; }

        public ILibraryManager LibraryManager { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public ILibraryHierarchyBrowser LibraryHierarchyBrowser { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.LibraryManager = core.Managers.Library;
            this.DatabaseFactory = core.Factories.Database;
            this.LibraryHierarchyBrowser = core.Components.LibraryHierarchyBrowser;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            base.InitializeComponent(core);
        }

        public async Task Build(LibraryItemStatus? status)
        {
            if (this.LibraryManager.State.HasFlag(LibraryManagerState.Updating))
            {
                return;
            }
            using (var task = new BuildLibraryHierarchiesTask(status))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public async Task Clear(LibraryItemStatus? status, bool signal)
        {
            if (this.LibraryManager.State.HasFlag(LibraryManagerState.Updating))
            {
                return;
            }
            using (var task = new ClearLibraryHierarchiesTask(status, signal))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }

        public Task<bool> Refresh(IEnumerable<IFileData> fileDatas, IEnumerable<string> names)
        {
            if (!this.RefreshRequired(fileDatas))
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.FromResult(false);
#endif
            }
            return this.Refresh(names);
        }

        protected virtual bool RefreshRequired(IEnumerable<IFileData> fileDatas)
        {
            foreach (var fileData in fileDatas)
            {
                if (fileData is LibraryItem)
                {
                    return true;
                }
                if (fileData is PlaylistItem playlistItem)
                {
                    if (playlistItem.LibraryItem_Id.HasValue)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task<bool> Refresh(IEnumerable<string> names)
        {
            if (!this.RefreshRequired(names))
            {
                return false;
            }
            await this.Clear(LibraryItemStatus.Import, false).ConfigureAwait(false);
            await this.Build(LibraryItemStatus.Import).ConfigureAwait(false);
            await this.LibraryManager.SetStatus(LibraryItemStatus.None).ConfigureAwait(false);
            return true;
        }

        protected virtual bool RefreshRequired(IEnumerable<string> names)
        {
            if (names == null || !names.Any())
            {
                //We don't know what has changed so assume the worst.
                return true;
            }
            foreach (var libraryHierarchy in this.LibraryHierarchyBrowser.GetHierarchies())
            {
                if (!libraryHierarchy.Enabled || libraryHierarchy.Type != LibraryHierarchyType.Script)
                {
                    //No need to refresh disabled hierarchies, not sure about non script types though. We should ask their plugin.
                    continue;
                }
                foreach (var libraryHierarchyLevel in libraryHierarchy.Levels)
                {
                    if (string.IsNullOrEmpty(libraryHierarchyLevel.Script))
                    {
                        continue;
                    }
                    //Very naive check whether the script references the meta data that has changed.
                    if (names.Any(name => libraryHierarchyLevel.Script.Contains(name, true)))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public string Checksum
        {
            get
            {
                return "C1399216-D828-4EB8-9249-70DEA89EADFA";
            }
        }

        public void InitializeDatabase(IDatabaseComponent database, DatabaseInitializeType type)
        {
            //IMPORTANT: When editing this function remember to change the checksum.
            if (!type.HasFlag(DatabaseInitializeType.Library))
            {
                return;
            }
            var scriptingRuntime = ComponentRegistry.Instance.GetComponent<IScriptingRuntime>();
            if (scriptingRuntime == null)
            {
                return;
            }
            using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
            {
                var set = database.Set<LibraryHierarchy>(transaction);
                foreach (var libraryHierarchy in set)
                {
                    //TODO: Bad .Wait()
                    LibraryTaskBase.RemoveHierarchies(database, libraryHierarchy, null, transaction).Wait();
                }
                set.Clear();
                set.Add(new LibraryHierarchy()
                {
                    Name = "Artist/Album/Title",
                    Type = LibraryHierarchyType.Script,
                    Sequence = 0,
                    Enabled = true,
                    Levels = new ObservableCollection<LibraryHierarchyLevel>()
                    {
                        new LibraryHierarchyLevel() { Sequence = 0, Script = scriptingRuntime.CoreScripts.Artist },
                        new LibraryHierarchyLevel() { Sequence = 1, Script = scriptingRuntime.CoreScripts.Year_Album },
                        new LibraryHierarchyLevel() { Sequence = 2, Script = scriptingRuntime.CoreScripts.Disk_Track_Title }
                    }
                });
                set.Add(new LibraryHierarchy()
                {
                    Name = "Genre/Album/Title",
                    Type = LibraryHierarchyType.Script,
                    Sequence = 1,
                    Enabled = false,
                    Levels = new ObservableCollection<LibraryHierarchyLevel>()
                    {
                        new LibraryHierarchyLevel() { Sequence = 0, Script = scriptingRuntime.CoreScripts.Genre },
                        new LibraryHierarchyLevel() { Sequence = 1, Script = scriptingRuntime.CoreScripts.Year_Album },
                        new LibraryHierarchyLevel() { Sequence = 2, Script = scriptingRuntime.CoreScripts.Disk_Track_Title }
                    }
                });
                set.Add(new LibraryHierarchy()
                {
                    Name = "Genre/Rating/Artist - Title [BPM]",
                    Type = LibraryHierarchyType.Script,
                    Sequence = 2,
                    Enabled = false,
                    Levels = new ObservableCollection<LibraryHierarchyLevel>()
                    {
                        new LibraryHierarchyLevel() { Sequence = 0, Script = scriptingRuntime.CoreScripts.Genre },
                        new LibraryHierarchyLevel() { Sequence = 1, Script = scriptingRuntime.CoreScripts.Rating },
                        new LibraryHierarchyLevel() { Sequence = 2, Script = scriptingRuntime.CoreScripts.Artist_Title_BPM }
                    }
                });
                set.Add(new LibraryHierarchy()
                {
                    Name = "Folder Structure",
                    Type = LibraryHierarchyType.FileSystem,
                    Sequence = 3,
                    Enabled = false
                });
                transaction.Commit();
            }
        }
    }
}
