using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class SelectionFollowsPlaybackBehaviour : StandardBehaviour, IInvocableComponent, IConfigurableComponent
    {
        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement SelectionFollowsPlayback { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.Configuration = core.Components.Configuration;
            this.SelectionFollowsPlayback = this.Configuration.GetElement<BooleanConfigurationElement>(
                SelectionFollowsPlaybackBehaviourConfiguration.SECTION,
                SelectionFollowsPlaybackBehaviourConfiguration.SELECTION_FOLLOWS_PLAYBACK
            );
            this.SelectionFollowsPlayback.ValueChanged += this.OnValueChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        public void Refresh()
        {
            if (!this.SelectionFollowsPlayback.Value)
            {
                return;
            }
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream == null)
            {
                return;
            }
            if (outputStream.PlaylistItem == null)
            {
                return;
            }
            var playlistItems = new[]
            {
                outputStream.PlaylistItem
            };
            if (this.PlaylistManager.SelectedItems != null && this.PlaylistManager.SelectedItems.SequenceEqual(playlistItems))
            {
                return;
            }
            this.PlaylistManager.SelectedItems = playlistItems;
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
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_PLAYLIST,
                    this.SelectionFollowsPlayback.Id,
                    this.SelectionFollowsPlayback.Name,
                    path: Strings.PlaylistBehaviour_Order,
                    attributes: (byte)((this.SelectionFollowsPlayback.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE) | InvocationComponent.ATTRIBUTE_SEPARATOR)
                );
                yield return new InvocationComponent(
                    InvocationComponent.CATEGORY_PLAYBACK,
                    this.SelectionFollowsPlayback.Id,
                    this.SelectionFollowsPlayback.Name,
                    path: Strings.PlaylistBehaviour_Order,
                    attributes: (byte)((this.SelectionFollowsPlayback.Value ? InvocationComponent.ATTRIBUTE_SELECTED : InvocationComponent.ATTRIBUTE_NONE) | InvocationComponent.ATTRIBUTE_SEPARATOR)
                );
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            this.SelectionFollowsPlayback.Toggle();
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return SelectionFollowsPlaybackBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
