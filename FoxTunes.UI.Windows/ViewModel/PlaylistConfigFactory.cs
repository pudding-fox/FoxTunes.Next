using System;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public static class PlaylistConfigFactory
    {
        public static PlaylistConfigBase Create(Playlist playlist)
        {
            switch (playlist.Type)
            {
                case PlaylistType.Dynamic:
                    return new DynamicPlaylistConfig(playlist);
                case PlaylistType.Smart:
                    return new SmartPlaylistConfig(playlist);
            }
            return default(PlaylistConfigBase);
        }
    }

    public abstract class PlaylistConfigBase : ViewModelBase
    {
        protected PlaylistConfigBase(Playlist playlist)
        {
            this.Playlist = playlist;
            this.Config = new FoxTunes.PlaylistConfig(this.Playlist);
        }

        public Playlist Playlist { get; private set; }

        public global::FoxTunes.PlaylistConfig Config { get; private set; }
    }

    public class DynamicPlaylistConfig : PlaylistConfigBase
    {
        public DynamicPlaylistConfig(Playlist playlist) : base(playlist)
        {

        }

        public string Expression
        {
            get
            {
                return this.Config.GetValueOrDefault(nameof(Expression), DynamicPlaylistBehaviour.DefaultExpression);
            }
            set
            {
                this.Config[nameof(Expression)] = value;
                this.OnExpressionChanged();
            }
        }

        protected virtual void OnExpressionChanged()
        {
            this.Config.Save();
            if (this.ExpressionChanged != null)
            {
                this.ExpressionChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Expression");
        }

        public event EventHandler ExpressionChanged;

        protected override Freezable CreateInstanceCore()
        {
            return new DynamicPlaylistConfig(null);
        }
    }

    public class SmartPlaylistConfig : PlaylistConfigBase
    {
        public SmartPlaylistConfig(Playlist playlist) : base(playlist)
        {

        }

        public string Genres
        {
            get
            {
                return this.Config.GetValueOrDefault(nameof(Genres), Convert.ToString(SmartPlaylistBehaviour.DefaultGenres));
            }
            set
            {
                this.Config[nameof(Genres)] = value;
                this.OnGenresChanged();
            }
        }

        protected virtual void OnGenresChanged()
        {
            this.Config.Save();
            if (this.GenresChanged != null)
            {
                this.GenresChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Genres");
        }

        public event EventHandler GenresChanged;

        public bool Like
        {
            get
            {
                return Convert.ToBoolean(this.Config.GetValueOrDefault(nameof(Like), Convert.ToString(SmartPlaylistBehaviour.DefaultLike)));
            }
            set
            {
                this.Config[nameof(Like)] = Convert.ToString(value);
                this.OnLikeChanged();
            }
        }

        protected virtual void OnLikeChanged()
        {
            this.Config.Save();
            if (this.LikeChanged != null)
            {
                this.LikeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Like");
        }

        public event EventHandler LikeChanged;

        public int MinRating
        {
            get
            {
                return Convert.ToInt32(this.Config.GetValueOrDefault(nameof(MinRating), Convert.ToString(SmartPlaylistBehaviour.DefaultMinRating)));
            }
            set
            {
                this.Config[nameof(MinRating)] = Convert.ToString(value);
                this.OnMinRatingChanged();
            }
        }

        protected virtual void OnMinRatingChanged()
        {
            this.Config.Save();
            if (this.MinRatingChanged != null)
            {
                this.MinRatingChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MinRating");
        }

        public event EventHandler MinRatingChanged;

        public int MinAge
        {
            get
            {
                return Convert.ToInt32(this.Config.GetValueOrDefault(nameof(MinAge), Convert.ToString(SmartPlaylistBehaviour.DefaultMinAge)));
            }
            set
            {
                this.Config[nameof(MinAge)] = Convert.ToString(value);
                this.OnMinAgeChanged();
            }
        }

        protected virtual void OnMinAgeChanged()
        {
            this.Config.Save();
            if (this.MinAgeChanged != null)
            {
                this.MinAgeChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MinAge");
        }

        public event EventHandler MinAgeChanged;

        public int Count
        {
            get
            {
                return Convert.ToInt32(this.Config.GetValueOrDefault(nameof(Count), Convert.ToString(SmartPlaylistBehaviour.DefaultCount)));
            }
            set
            {
                this.Config[nameof(Count)] = Convert.ToString(value);
                this.OnCountChanged();
            }
        }

        protected virtual void OnCountChanged()
        {
            this.Config.Save();
            if (this.CountChanged != null)
            {
                this.CountChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Count");
        }

        public event EventHandler CountChanged;

        protected override Freezable CreateInstanceCore()
        {
            return new SmartPlaylistConfig(null);
        }
    }
}
