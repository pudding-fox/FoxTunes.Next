using FoxTunes.AI.Tasks;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    [ComponentDependency(Slot = ComponentSlots.AIRuntime)]
    public class AILibraryBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent
    {
        public ICore Core { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public TextConfigurationElement FileId { get; private set; }

        public TextConfigurationElement VectorStoreId { get; private set; }

        public CommandConfigurationElement Update { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.SignalEmitter = core.Components.SignalEmitter;
            this.SignalEmitter.Signal += this.OnSignal;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                AIBehaviourConfiguration.SECTION,
                AIBehaviourConfiguration.ENABLED
            );
            this.FileId = this.Configuration.GetElement<TextConfigurationElement>(
                AIBehaviourConfiguration.SECTION,
                AIBehaviourConfiguration.FILE_ID
            );
            this.VectorStoreId = this.Configuration.GetElement<TextConfigurationElement>(
                AIBehaviourConfiguration.SECTION,
                AIBehaviourConfiguration.VECTOR_STORE_ID
            );
            this.Update = this.Configuration.GetElement<CommandConfigurationElement>(
                AILibraryBehaviourConfiguration.SECTION,
                AILibraryBehaviourConfiguration.UPDATE
            );
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
            if (!this.Enabled.Value)
            {
                //Nothing to do.
                return;
            }
            using (var task = new CreateAILibraryTask(this.FileId.Value, this.VectorStoreId.Value))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
                this.FileId.Value = task.FileId;
                this.VectorStoreId.Value = task.VectorStoreId;
            }
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_LIBRARY;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.Enabled.Value)
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_LIBRARY, this.Update.Id, this.Update.Name, path: Strings.AILibraryBehaviour_Update_Path);
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            if (component.Id == this.Update.Id)
            {
                this.Update.Invoke();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return AILibraryBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
