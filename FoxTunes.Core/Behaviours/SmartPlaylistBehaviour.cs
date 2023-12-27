using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class SmartPlaylistBehaviour : PlaylistBehaviourBase
    {
        public const string MinRating = "MinRating";

        public const string MinAge = "MinAge";

        public const string Count = "Count";

        public const int DefaultMinRating = 4;

        public const int DefaultMinAge = 30;

        public const int DefaultCount = 16;

        public override Func<Playlist, bool> Predicate
        {
            get
            {
                return playlist => playlist.Type == PlaylistType.Smart && playlist.Enabled;
            }
        }

        protected virtual void GetConfig(Playlist playlist, out string expression, out int count)
        {
            var config = this.GetConfig(playlist);
            var minRating = default(int);
            var minAge = default(int);
            if (!int.TryParse(config.GetValueOrDefault(MinRating), out minRating))
            {
                minRating = DefaultMinRating;
            }
            if (!int.TryParse(config.GetValueOrDefault(MinAge), out minAge))
            {
                minAge = DefaultMinAge;
            }
            if (!int.TryParse(config.GetValueOrDefault(Count), out count))
            {
                count = DefaultCount;
            }
            expression = string.Format(
                "rating>:{0} lastplayed<{1}",
                minRating,
                DateTimeHelper.ToShortString(DateTime.Now.AddDays(minAge * -1).Date)
            );
        }

        public ICore Core { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            base.InitializeComponent(core);
        }

        public override Task Refresh(Playlist playlist, bool force)
        {
            var expression = default(string);
            var count = default(int);
            this.GetConfig(playlist, out expression, out count);
            return this.Refresh(playlist, expression, count, force);
        }

        protected virtual async Task Refresh(Playlist playlist, string expression, int count, bool force)
        {
            if (!force)
            {
                //Only refresh when user requests.
                return;
            }
            using (var task = new CreateSmartPlaylistTask(playlist, expression, "random", count))
            {
                task.InitializeComponent(this.Core);
                await task.Run().ConfigureAwait(false);
            }
        }
    }
}
