using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes.AI
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class AIPlaylistBehaviour : PlaylistBehaviourBase
    {
        public const string Prompt = "Genres";

        public const string DefaultPrompt = "";

        public override Func<Playlist, bool> Predicate
        {
            get
            {
                return playlist => playlist.Type == PlaylistType.AI && playlist.Enabled;
            }
        }

        protected virtual void GetConfig(Playlist playlist, out string prompt)
        {
            var config = this.GetConfig(playlist);
            if (!config.TryGetValue(Prompt, out prompt))
            {
                prompt = DefaultPrompt;
            }
        }

        public ICore Core { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            base.InitializeComponent(core);
        }

        public override Task Refresh(Playlist playlist, bool force)
        {
            var prompt = default(string);
            this.GetConfig(playlist, out prompt);
            return this.Refresh(playlist, prompt, force);
        }

        protected virtual async Task Refresh(Playlist playlist, string prompt, bool force)
        {
            if (!force)
            {
                //Only refresh when user requests.
                return;
            }
            using (var task = new CreateAIPlaylistTask(playlist, prompt))
            {
                task.InitializeComponent(this.Core);
                await task.Run().ConfigureAwait(false);
            }
        }
    }
}
