using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class StreamPositionBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent
    {
        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement ShowCounters { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.ShowCounters = this.Configuration.GetElement<BooleanConfigurationElement>(
                StreamPositionBehaviourConfiguration.SECTION,
                StreamPositionBehaviourConfiguration.SHOW_COUNTERS_ELEMENT
            );
            base.InitializeComponent(core);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_STREAM_POSITION;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_STREAM_POSITION,
                    this.ShowCounters.Id,
                    this.ShowCounters.Name,
                    this.ShowCounters.Description,
                    attributes: this.ShowCounters.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                );
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            if (string.Equals(component.Id, this.ShowCounters.Id, StringComparison.OrdinalIgnoreCase))
            {
                this.ShowCounters.Toggle();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return StreamPositionBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
