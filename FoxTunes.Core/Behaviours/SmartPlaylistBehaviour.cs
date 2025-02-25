using FoxTunes.Interfaces;
using System;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class SmartPlaylistBehaviour : PlaylistBehaviourBase
    {
        public const string Genres = "Genres";

        public const string Like = "Like";

        public const string MinRating = "MinRating";

        public const string MinAge = "MinAge";

        public const string Count = "Count";

        public const string DefaultGenres = "";

        public const bool DefaultLike = false;

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
            var genres = default(string);
            var like = default(bool);
            var minRating = default(int);
            var minAge = default(int);
            if (!config.TryGetValue(Genres, out genres))
            {
                genres = DefaultGenres;
            }
            if (!bool.TryParse(config.GetValueOrDefault(Like), out like))
            {
                like = DefaultLike;
            }
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

            var builder = new StringBuilder();
            if (!string.IsNullOrEmpty(genres))
            {
                foreach (var genre in genres.Split('\t'))
                {
                    this.Append(builder, string.Concat("genre:", genre));
                }
            }
            if (like)
            {
                this.Append(builder, "like");
            }
            if (minRating > 0)
            {
                this.Append(builder, string.Concat("rating>:", minRating));
            }
            if (minAge > 0)
            {
                this.Append(builder, string.Concat("lastplayed<", DateTimeHelper.ToShortString(DateTime.Now.AddDays(minAge * -1).Date)));
            }
            expression = builder.ToString();
        }

        protected virtual void Append(StringBuilder builder, string filter)
        {
            if (builder.Length > 0)
            {
                builder.Append(" ");
            }
            builder.Append(filter);
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
