using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class LibrarySettings : ViewModelBase
    {
        public ILibraryManager LibraryManager { get; private set; }

        public IHierarchyManager HierarchyManager { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        private CollectionManager<LibraryHierarchy> _LibraryHierarchies { get; set; }

        public CollectionManager<LibraryHierarchy> LibraryHierarchies
        {
            get
            {
                return this._LibraryHierarchies;
            }
            set
            {
                this._LibraryHierarchies = value;
                this.OnLibraryHierarchiesChanged();
            }
        }

        protected virtual void OnLibraryHierarchiesChanged()
        {
            this.OnPropertyChanged("LibraryHierarchies");
        }

        private CollectionManager<LibraryHierarchyLevel> _LibraryHierarchyLevels { get; set; }

        public CollectionManager<LibraryHierarchyLevel> LibraryHierarchyLevels
        {
            get
            {
                return this._LibraryHierarchyLevels;
            }
            set
            {
                this._LibraryHierarchyLevels = value;
                this.OnLibraryHierarchyLevelsChanged();
            }
        }

        protected virtual void OnLibraryHierarchyLevelsChanged()
        {
            this.OnPropertyChanged("LibraryHierarchyLevels");
        }

        private bool _SettingsVisible { get; set; }

        public bool SettingsVisible
        {
            get
            {
                return this._SettingsVisible;
            }
            set
            {
                this._SettingsVisible = value;
                this.OnSettingsVisibleChanged();
            }
        }

        protected virtual void OnSettingsVisibleChanged()
        {
            if (this.SettingsVisibleChanged != null)
            {
                this.SettingsVisibleChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SettingsVisible");
        }

        public event EventHandler SettingsVisibleChanged;

        public bool IsSaving
        {
            get
            {
                return global::FoxTunes.BackgroundTask.Active
                    .OfType<LibraryTaskBase>()
                    .Any();
            }
        }

        protected virtual void OnIsSavingChanged()
        {
            if (this.IsSavingChanged != null)
            {
                this.IsSavingChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsSaving");
        }

        public event EventHandler IsSavingChanged;

        public ICommand SaveCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Save);
            }
        }

        public async Task Save()
        {
            var exception = default(Exception);
            try
            {
                using (var database = this.DatabaseFactory.Create())
                {
                    using (var task = new SingletonReentrantTask(CancellationToken.None, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
                    {
                        using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                        {
                            var libraryHierarchies = database.Set<LibraryHierarchy>(transaction);
                            foreach (var libraryHierarchy in this.LibraryHierarchies.Removed)
                            {
                                await LibraryTaskBase.RemoveHierarchies(database, libraryHierarchy, null, transaction).ConfigureAwait(false);
                                await libraryHierarchies.RemoveAsync(libraryHierarchy).ConfigureAwait(false);
                            }
                            await libraryHierarchies.AddOrUpdateAsync(this.LibraryHierarchies.ItemsSource).ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }))
                    {
                        await task.Run().ConfigureAwait(false);
                    }
                }
                await this.Refresh().ConfigureAwait(false);
                await this.Rebuild().ConfigureAwait(false);
                return;
            }
            catch (Exception e)
            {
                exception = e;
            }
            await this.ErrorEmitter.Send(this, "Save", exception).ConfigureAwait(false);
            throw exception;
        }

        public async Task Rebuild()
        {
            await this.HierarchyManager.Clear(null, true).ConfigureAwait(false);
            await this.HierarchyManager.Build(null).ConfigureAwait(false);
        }

        public ICommand RescanCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(
                    new Func<Task>(this.Rescan)
                );
            }
        }

        public Task Rescan()
        {
            return this.LibraryManager.Rescan(false);
        }

        public ICommand ClearCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Clear);
            }
        }

        public async Task Clear()
        {
            await this.HierarchyManager.Clear(null, true).ConfigureAwait(false);
            await this.LibraryManager.Clear(null).ConfigureAwait(false);
        }

        public ICommand CancelCommand
        {
            get
            {
                return new Command(this.Cancel);
            }
        }

        public void Cancel()
        {
            this.Dispatch(this.Refresh);
        }

        public ICommand ResetCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Reset);
            }
        }

        public async Task Reset()
        {
            using (var database = this.DatabaseFactory.Create())
            {
                using (var task = new SingletonReentrantTask(CancellationToken.None, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, cancellationToken =>
                {
                    Core.Instance.InitializeDatabase(database, DatabaseInitializeType.Library);
#if NET40
                    return TaskEx.FromResult(false);
#else
                    return Task.CompletedTask;
#endif
                }))
                {
                    await task.Run().ConfigureAwait(false);
                }
            }
            await this.Refresh().ConfigureAwait(false);
            await this.HierarchyManager.Build(null).ConfigureAwait(false);
        }

        public ICommand HelpCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(this.Help);
            }
        }

        public void Help()
        {
            var fileName = Path.Combine(
                Path.GetTempPath(),
                "Scripting.txt"
            );
            File.WriteAllText(fileName, Resources.Scripting);
            Process.Start(fileName);
        }

        protected override void InitializeComponent(ICore core)
        {
            global::FoxTunes.BackgroundTask.ActiveChanged += this.OnActiveChanged;
            this.LibraryManager = core.Managers.Library;
            this.HierarchyManager = core.Managers.Hierarchy;
            this.DatabaseFactory = core.Factories.Database;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            this.LibraryHierarchyLevels = new CollectionManager<LibraryHierarchyLevel>()
            {
                ItemFactory = () =>
                {
                    using (var database = this.DatabaseFactory.Create())
                    {
                        return database.Set<LibraryHierarchyLevel>().Create().With(libraryHierarchyLevel =>
                        {
                            libraryHierarchyLevel.Script = "'New'";
                        });
                    }
                },
                ExchangeHandler = (item1, item2) =>
                {
                    var temp = item1.Sequence;
                    item1.Sequence = item2.Sequence;
                    item2.Sequence = temp;
                }
            };
            this.LibraryHierarchies = new CollectionManager<LibraryHierarchy>()
            {
                ItemFactory = () =>
                {
                    using (var database = this.DatabaseFactory.Create())
                    {
                        return database.Set<LibraryHierarchy>().Create().With(libraryHierarchy =>
                        {
                            libraryHierarchy.Name = "New";
                            libraryHierarchy.Type = LibraryHierarchyType.Script;
                        });
                    }
                },
                ExchangeHandler = (item1, item2) =>
                {
                    var temp = item1.Sequence;
                    item1.Sequence = item2.Sequence;
                    item2.Sequence = temp;
                },
                CloneHandler = item =>
                {
                    using (var database = this.DatabaseFactory.Create())
                    {
                        return database.Set<LibraryHierarchy>().Create().With(libraryHierarchy =>
                        {
                            libraryHierarchy.Name = string.Format("{0} (Copy)", item.Name);
                            libraryHierarchy.Type = item.Type;
                            libraryHierarchy.Enabled = item.Enabled;
                            foreach (var level in item.Levels)
                            {
                                libraryHierarchy.Levels.Add(database.Set<LibraryHierarchyLevel>().Create().With(libraryHierarchyLevel =>
                                {
                                    libraryHierarchyLevel.Sequence = level.Sequence;
                                    libraryHierarchyLevel.Script = level.Script;
                                }));
                            }
                        });
                    }
                }
            };
            this.LibraryHierarchies.SelectedValueChanged += this.OnSelectedValueChanged;
            this.Dispatch(this.Refresh);
            base.InitializeComponent(core);
        }

        protected virtual async void OnActiveChanged(object sender, EventArgs e)
        {
            await Windows.Invoke(() => this.OnIsSavingChanged()).ConfigureAwait(false);
        }

        private Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.SettingsUpdated:
                    return this.Refresh();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual void OnSelectedValueChanged(object sender, EventArgs e)
        {
            if (this.LibraryHierarchies.SelectedValue != null)
            {
                this.LibraryHierarchyLevels.ItemsSource = this.LibraryHierarchies.SelectedValue.Levels;
            }
            else
            {
                this.LibraryHierarchyLevels.ItemsSource = null;
            }
        }

        protected virtual Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                using (var database = this.DatabaseFactory.Create())
                {
                    using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                    {
                        this.LibraryHierarchies.ItemsSource = new ObservableCollection<LibraryHierarchy>(
                            database.Set<LibraryHierarchy>(transaction)
                        );
                    }
                }
            });
        }

        protected override void OnDisposing()
        {
            global::FoxTunes.BackgroundTask.ActiveChanged -= this.OnActiveChanged;
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
            if (this.LibraryHierarchies != null)
            {
                this.LibraryHierarchies.SelectedValueChanged -= this.OnSelectedValueChanged;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new LibrarySettings();
        }
    }
}
