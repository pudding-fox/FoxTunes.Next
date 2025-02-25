using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class PlaylistSettings : ViewModelBase
    {
        public IPlaylistColumnManager PlaylistColumnProviderManager { get; private set; }

        public IMetaDataSourceFactory SourceFactory { get; private set; }

        public IMetaDataDecoratorFactory DecoratorFactory { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public IPlaylistBrowser PlaylistBrowser { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        private CollectionManager<PlaylistColumn> _PlaylistColumns { get; set; }

        public CollectionManager<PlaylistColumn> PlaylistColumns
        {
            get
            {
                return this._PlaylistColumns;
            }
            set
            {
                this._PlaylistColumns = value;
                this.OnPlaylistColumnsChanged();
            }
        }

        protected virtual void OnPlaylistColumnsChanged()
        {
            this.OnPropertyChanged("PlaylistColumns");
        }

        private ObservableCollection<IPlaylistColumnProvider> _PlaylistColumnProviders { get; set; }

        public ObservableCollection<IPlaylistColumnProvider> PlaylistColumnProviders
        {
            get
            {
                return this._PlaylistColumnProviders;
            }
            set
            {
                this._PlaylistColumnProviders = value;
                this.OnPlaylistColumnProvidersChanged();
            }
        }

        protected virtual void OnPlaylistColumnProvidersChanged()
        {
            this.OnPropertyChanged("PlaylistColumnProviders");
        }

        public IEnumerable<SupportedMetaData> SupportedMetaData
        {
            get
            {
                if (this.SourceFactory == null)
                {
                    return Enumerable.Empty<SupportedMetaData>();
                }
                var supported = this.SourceFactory.Supported;
                if (this.DecoratorFactory.CanCreate)
                {
                    supported = supported.Concat(this.DecoratorFactory.Supported);
                }
                return supported.Select(element => new SupportedMetaData(element.Key, element.Value)).ToArray();
            }
        }

        protected virtual void OnSupportedMetaDataChanged()
        {
            if (this.SupportedMetaDataChanged != null)
            {
                this.SupportedMetaDataChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("SupportedMetaData");
        }

        public event EventHandler SupportedMetaDataChanged;

        public IEnumerable<string> SupportedFormats
        {
            get
            {
                return new[]
                {
                    CommonFormats.Decibel,
                    CommonFormats.Float,
                    CommonFormats.Integer,
                    CommonFormats.TimeSpan,
                    CommonFormats.TimeStamp,
                    CommonFormats.Size
                };
            }
        }

        public bool IsSaving
        {
            get
            {
                return global::FoxTunes.BackgroundTask.Active
                    .OfType<PlaylistTaskBase>()
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
                    using (var task = new SingletonReentrantTask(CancellationToken.None, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, cancellationToken =>
                    {
                        using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                        {
                            var playlistColumns = database.Set<PlaylistColumn>(transaction);
                            playlistColumns.Remove(playlistColumns.Except(this.PlaylistColumns.ItemsSource));
                            playlistColumns.AddOrUpdate(this.PlaylistColumns.ItemsSource);
                            transaction.Commit();
                        }
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
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistColumnsUpdated)).ConfigureAwait(false);
                return;
            }
            catch (Exception e)
            {
                exception = e;
            }
            await this.ErrorEmitter.Send(this, "Save", exception).ConfigureAwait(false);
            throw exception;
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
                    Core.Instance.InitializeDatabase(database, DatabaseInitializeType.Playlist);
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
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated)).ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistColumnsUpdated)).ConfigureAwait(false);
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
            this.PlaylistColumnProviderManager = ComponentRegistry.Instance.GetComponent<PlaylistColumnManager>();
            this.PlaylistBrowser = core.Components.PlaylistBrowser;
            this.PlaylistManager = core.Managers.Playlist;
            this.DatabaseFactory = core.Factories.Database;
            this.SourceFactory = core.Factories.MetaDataSource;
            this.DecoratorFactory = core.Factories.MetaDataDecorator;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            this.PlaylistColumns = new CollectionManager<PlaylistColumn>()
            {
                ItemFactory = () =>
                {
                    using (var database = this.DatabaseFactory.Create())
                    {
                        return database.Set<PlaylistColumn>().Create().With(playlistColumn =>
                        {
                            playlistColumn.Name = "New";
                            playlistColumn.Type = PlaylistColumnType.Tag;
                            playlistColumn.Script = "'New'";
                            playlistColumn.Enabled = true;
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
            this.PlaylistColumnProviders = new ObservableCollection<IPlaylistColumnProvider>(
                this.PlaylistColumnProviderManager.Providers
            );
            this.Dispatch(this.Refresh);
            base.InitializeComponent(core);
        }

        protected virtual async void OnActiveChanged(object sender, EventArgs e)
        {
            await Windows.Invoke(() => this.OnIsSavingChanged()).ConfigureAwait(false);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.SettingsUpdated:
                    return this.Refresh();
                case CommonSignals.PlaylistColumnsUpdated:
                    return this.OnPlaylistColumnsUpdated(signal.State as PlaylistColumnsUpdatedSignalState);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                this.PlaylistColumns.ItemsSource = new ObservableCollection<PlaylistColumn>(
                    this.PlaylistBrowser.GetColumns()
                );
                this.OnSupportedMetaDataChanged();
            });
        }

        protected virtual Task OnPlaylistColumnsUpdated(PlaylistColumnsUpdatedSignalState state)
        {
            if (state != null && state.Columns != null && state.Columns.Any())
            {
                return Windows.Invoke(this.PlaylistColumns.Refresh);
            }
            else
            {
                return this.Refresh();
            }
        }

        protected override void OnDisposing()
        {
            global::FoxTunes.BackgroundTask.ActiveChanged -= this.OnActiveChanged;
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new PlaylistSettings();
        }
    }

    public class SupportedMetaData
    {
        public SupportedMetaData(string name, MetaDataItemType type) : this(GetSequence(type), name, type)
        {

        }

        public SupportedMetaData(int sequence, string name, MetaDataItemType type)
        {
            this.Sequence = sequence;
            this.Name = name;
            this.Type = type;
        }

        public int Sequence { get; private set; }

        public string Name { get; private set; }

        public MetaDataItemType Type { get; private set; }

        private static int GetSequence(MetaDataItemType type)
        {
            switch (type)
            {
                case MetaDataItemType.Tag:
                    return 0;
                case MetaDataItemType.CustomTag:
                    return 1;
                case MetaDataItemType.Property:
                    return 2;
                case MetaDataItemType.Statistic:
                    return 3;
                default:
                    return 10;
            }
        }
    }
}
