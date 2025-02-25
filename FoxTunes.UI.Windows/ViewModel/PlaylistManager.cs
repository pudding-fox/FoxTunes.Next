using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class PlaylistManager : Playlists
    {
        public PlaylistManager()
        {
            this.WindowState = new WindowState(PlaylistManagerWindow.ID);
        }

        public WindowState WindowState { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        public override bool EnabledOnly
        {
            get
            {
                return false;
            }
        }

        protected override void InitializeComponent(ICore core)
        {
            global::FoxTunes.BackgroundTask.ActiveChanged += this.OnActiveChanged;
            this.DatabaseFactory = core.Factories.Database;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            base.InitializeComponent(core);
        }

        protected virtual async void OnActiveChanged(object sender, EventArgs e)
        {
            await Windows.Invoke(() => this.OnIsSavingChanged()).ConfigureAwait(false);
        }

        public bool PlaylistManagerVisible
        {
            get
            {
                return Windows.Registrations.IsVisible(PlaylistManagerWindow.ID);
            }
            set
            {
                if (value)
                {
                    Windows.Registrations.Show(PlaylistManagerWindow.ID);
                }
                else
                {
                    Windows.Registrations.Hide(PlaylistManagerWindow.ID);
                }
            }
        }

        protected virtual void OnPlaylistManagerVisibleChanged()
        {
            if (this.PlaylistManagerVisibleChanged != null)
            {
                this.PlaylistManagerVisibleChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("PlaylistManagerVisible");
        }

        public event EventHandler PlaylistManagerVisibleChanged;

        public ICommand MoveUpCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand<Playlist>(this.MoveUp);
            }
        }

        public Task MoveUp(Playlist playlist)
        {
            var playlist1 = playlist;
            var playlist2 = this.Items.BySequence(playlist1.Sequence - 1);
            if (playlist2 == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var temp = playlist1.Sequence;
            playlist1.Sequence = playlist2.Sequence;
            playlist2.Sequence = temp;
            return this.Save();
        }

        public ICommand MoveDownCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand<Playlist>(this.MoveDown);
            }
        }

        public Task MoveDown(Playlist playlist)
        {
            var playlist1 = playlist;
            var playlist2 = this.Items.BySequence(playlist1.Sequence + 1);
            if (playlist2 == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var temp = playlist1.Sequence;
            playlist1.Sequence = playlist2.Sequence;
            playlist2.Sequence = temp;
            return this.Save();
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
                    using (var task = new SingletonReentrantTask(CancellationToken.None, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
                    {
                        using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                        {
                            var playlists = database.Set<Playlist>(transaction);
                            await playlists.AddOrUpdateAsync(this.Items).ConfigureAwait(false);
                            transaction.Commit();
                        }
                    }))
                    {
                        await task.Run().ConfigureAwait(false);
                    }
                }
                await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated)).ConfigureAwait(false);
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

        protected override void OnDisposing()
        {
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new PlaylistManager();
        }
    }
}
