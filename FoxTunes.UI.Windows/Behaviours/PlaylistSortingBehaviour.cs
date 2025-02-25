using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class PlaylistSortingBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent, IDisposable
    {
        public const string TOGGLE_SORTING = "NNNN";

        public PlaylistColumn SortColumn { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Sorting { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.Configuration = core.Components.Configuration;
            this.Sorting = this.Configuration.GetElement<BooleanConfigurationElement>(
                PlaylistBehaviourConfiguration.SECTION,
                PlaylistSortingBehaviourConfiguration.SORT_ENABLED_ELEMENT
            );
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.PlaylistUpdated:
                    this.SortColumn = null;
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_PLAYLIST_HEADER;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_PLAYLIST_HEADER,
                    TOGGLE_SORTING,
                    Strings.PlaylistSortingBehaviourConfiguration_Enabled,
                    attributes: this.Sorting.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case TOGGLE_SORTING:
                    this.Sorting.Toggle();
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public async Task Sort(Playlist playlist, PlaylistColumn playlistColumn)
        {
            if (!this.Sorting.Value)
            {
                return;
            }
            var descending = default(bool);
            if (object.ReferenceEquals(this.SortColumn, playlistColumn))
            {
                descending = true;
            }
            var changes = await this.PlaylistManager.Sort(playlist, playlistColumn, descending).ConfigureAwait(false);
            if (changes == 0)
            {
                Logger.Write(this, LogLevel.Debug, "Playlist was already sorted, reversing direction.");
                descending = !descending;
                changes = await this.PlaylistManager.Sort(playlist, playlistColumn, descending).ConfigureAwait(false);
                if (changes == 0)
                {
                    Logger.Write(this, LogLevel.Debug, "Playlist was already sorted, all values are equal.");
                }
            }
            if (!descending)
            {
                this.SortColumn = playlistColumn;
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return PlaylistSortingBehaviourConfiguration.GetConfigurationSections();
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            if (this.SignalEmitter != null)
            {
                this.SignalEmitter.Signal -= this.OnSignal;
            }
        }

        ~PlaylistSortingBehaviour()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}