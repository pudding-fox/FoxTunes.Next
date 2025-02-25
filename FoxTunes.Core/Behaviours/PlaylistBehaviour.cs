using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class PlaylistBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent
    {
        public IConfiguration Configuration { get; private set; }

        public SelectionConfigurationElement Order { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Order = this.Configuration.GetElement<SelectionConfigurationElement>(
                PlaylistBehaviourConfiguration.SECTION,
                PlaylistBehaviourConfiguration.ORDER_ELEMENT
            );
            base.InitializeComponent(core);
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_PLAYLIST;
                yield return InvocationComponent.CATEGORY_PLAYBACK;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                foreach (var option in this.Order.Options)
                {
                    yield return new InvocationComponent(
                        InvocationComponent.CATEGORY_PLAYLIST,
                        option.Id,
                        option.Name,
                        path: Strings.PlaylistBehaviour_Order,
                        attributes: string.Equals(option.Id, this.Order.Value.Id, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                    );
                }
                foreach (var option in this.Order.Options)
                {
                    yield return new InvocationComponent(
                        InvocationComponent.CATEGORY_PLAYBACK,
                        option.Id,
                        option.Name,
                        path: Strings.PlaylistBehaviour_Order,
                        attributes: string.Equals(option.Id, this.Order.Value.Id, StringComparison.OrdinalIgnoreCase) ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE
                    );
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case PlaylistBehaviourConfiguration.ORDER_DEFAULT_OPTION:
                case PlaylistBehaviourConfiguration.ORDER_SHUFFLE_TRACKS:
                case PlaylistBehaviourConfiguration.ORDER_SHUFFLE_ALBUMS:
                case PlaylistBehaviourConfiguration.ORDER_SHUFFLE_ARTISTS:
                    this.Order.Value = this.Order.GetOption(component.Id);
                    break;
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return PlaylistBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
