using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class MetaDataProvidersSettings : ViewModelBase
    {
        public IMetaDataProviderManager MetaDataProviderManager { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        private CollectionManager<MetaDataProvider> _MetaDataProviders { get; set; }

        public CollectionManager<MetaDataProvider> MetaDataProviders
        {
            get
            {
                return this._MetaDataProviders;
            }
            set
            {
                this._MetaDataProviders = value;
                this.OnMetaDataProvidersChanged();
            }
        }

        protected virtual void OnMetaDataProvidersChanged()
        {
            this.OnPropertyChanged("MetaDataProviders");
        }

        public bool IsSaving
        {
            get
            {
                return global::FoxTunes.BackgroundTask.Active
                    .Any(task => task is PlaylistTaskBase || task is LibraryTaskBase);
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
                            var metaDataProviders = database.Set<MetaDataProvider>(transaction);
                            metaDataProviders.Remove(metaDataProviders.Except(this.MetaDataProviders.ItemsSource));
                            metaDataProviders.AddOrUpdate(this.MetaDataProviders.ItemsSource);
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
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.MetaDataProvidersUpdated)).ConfigureAwait(false);
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
                    Core.Instance.InitializeDatabase(database, DatabaseInitializeType.MetaData);
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
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.MetaDataProvidersUpdated)).ConfigureAwait(false);
        }

        protected override void InitializeComponent(ICore core)
        {
            global::FoxTunes.BackgroundTask.ActiveChanged += this.OnActiveChanged;
            this.MetaDataProviderManager = core.Managers.MetaDataProvider;
            this.DatabaseFactory = core.Factories.Database;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            this.MetaDataProviders = new CollectionManager<MetaDataProvider>(CollectionManagerFlags.AllowEmptyCollection)
            {
                ItemFactory = () =>
                {
                    using (var database = this.DatabaseFactory.Create())
                    {
                        return database.Set<MetaDataProvider>().Create().With(metaDataProvider =>
                        {
                            metaDataProvider.Name = "New";
                            metaDataProvider.Type = MetaDataProviderType.Script;
                            metaDataProvider.Script = "'New'";
                            metaDataProvider.Enabled = true;
                        });
                    }
                }
            };
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
                case CommonSignals.MetaDataProvidersUpdated:
                    var metaDataProviders = signal.State as IEnumerable<MetaDataProvider>;
                    if (metaDataProviders != null && metaDataProviders.Any())
                    {
                        this.MetaDataProviders.Refresh();
                    }
                    else
                    {
                        return this.Refresh();
                    }
                    break;
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
                this.MetaDataProviders.ItemsSource = new ObservableCollection<MetaDataProvider>(
                    this.MetaDataProviderManager.GetProviders()
                );
            });
        }

        protected override void OnDisposing()
        {
            global::FoxTunes.BackgroundTask.ActiveChanged -= this.OnActiveChanged;
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new MetaDataProvidersSettings();
        }
    }
}
