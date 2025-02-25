using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    [ComponentDependency(Slot = ComponentSlots.AIRuntime)]
    public class AIDJPlaylistProvider : PlaylistProvider
    {
        public const string History = "History";

        public const int DefaultHistory = 10;

        public const string Count = "Count";

        public const int DefaultCount = 10;

        public override Func<Playlist, bool> Predicate
        {
            get
            {
                return playlist => playlist.Type == PlaylistType.AIDJ && playlist.Enabled;
            }
        }
        protected virtual void GetConfig(Playlist playlist, out int history, out int count)
        {
            var config = this.GetConfig(playlist);
            if (!int.TryParse(config.GetOrAdd(History, Convert.ToString(DefaultHistory)), out history))
            {
                history = DefaultHistory;
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
            var history = default(int);
            var count = default(int);
            this.GetConfig(playlist, out history, out count);
            return this.Refresh(playlist, history, count, force);
        }

        protected virtual async Task Refresh(Playlist playlist, int history, int count, bool force)
        {
            if (!force)
            {
                //Only refresh when user requests.
                return;
            }
            using (var task = new CreateAIDJPlaylistTask(playlist, history, count))
            {
                task.InitializeComponent(this.Core);
                await this.BackgroundTaskEmitter.Send(task).ConfigureAwait(false);
                await task.Run().ConfigureAwait(false);
            }
        }
    }
}
