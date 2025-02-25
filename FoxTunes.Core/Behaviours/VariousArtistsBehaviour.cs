using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    //Setting PRIORITY_HIGH so the library is updated before hiearchies are build etc.
    [ComponentPriority(ComponentPriorityAttribute.HIGH)]
    public class VariousArtistsBehaviour : StandardBehaviour
    {
        public ICore Core { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement DetectCompilations { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.DatabaseFactory = core.Factories.Database;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.Configuration = core.Components.Configuration;
            this.DetectCompilations = this.Configuration.GetElement<BooleanConfigurationElement>(
                MetaDataBehaviourConfiguration.SECTION,
                MetaDataBehaviourConfiguration.DETECT_COMPILATIONS
            );
            this.DetectCompilations.ValueChanged += this.OnValueChanged;
            base.InitializeComponent(core);
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            switch (signal.Name)
            {
                case CommonSignals.LibraryUpdated:
                    if (signal.State is LibraryUpdatedSignalState libraryUpdatedSignalState)
                    {
                        return this.Refresh(libraryUpdatedSignalState);
                    }
                    else
                    {
                        return this.Refresh();
                    }
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            var task = this.Refresh();
        }

        public Task Refresh(LibraryUpdatedSignalState state)
        {
            if (state != null && state.LibraryItems != null && state.LibraryItems.Any())
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Refresh();
        }

        public async Task Refresh()
        {
            using (var task = new UpdateVariousArtistsTask(this.DetectCompilations.Value))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }
    }
}
