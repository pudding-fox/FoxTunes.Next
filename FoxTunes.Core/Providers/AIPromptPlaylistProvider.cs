using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    [ComponentDependency(Slot = ComponentSlots.AIRuntime)]
    public class AIPromptPlaylistProvider : PlaylistProvider
    {
        public const string Prompt = "Prompt";

        public const string DefaultPrompt = "energetic";

        public const string Count = "Count";

        public const int DefaultCount = 10;

        public override Func<Playlist, bool> Predicate
        {
            get
            {
                return playlist => playlist.Type == PlaylistType.AIPrompt && playlist.Enabled;
            }
        }

        protected virtual void GetConfig(Playlist playlist, out string prompt, out int count)
        {
            var config = this.GetConfig(playlist);
            prompt = config.GetValueOrDefault(Prompt, DefaultPrompt);
            if (string.IsNullOrEmpty(prompt))
            {
                prompt = DefaultPrompt;
            }
            if (!int.TryParse(config.GetOrAdd(Count, Convert.ToString(DefaultCount)), out count))
            {
                count = DefaultCount;
            }
        }

        public ICore Core { get; private set; }

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            base.InitializeComponent(core);
        }

        public override Task Refresh(Playlist playlist, bool force)
        {
            var prompt = default(string);
            var count = default(int);
            this.GetConfig(playlist, out prompt, out count);
            return this.Refresh(playlist, prompt, count, force);
        }

        protected virtual async Task Refresh(Playlist playlist, string prompt, int count, bool force)
        {
            if (!force)
            {
                //Only refresh when user requests.
                return;
            }
            using (var task = new CreateAIPromptPlaylistTask(playlist, prompt, count))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }
    }
}
