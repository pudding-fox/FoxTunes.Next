using FoxTunes.Interfaces;
using System;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Play : ViewModelBase
    {
        public Play()
        {
            PlaybackStateNotifier.Notify += this.OnNotify;
        }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        private bool _IsPlaying { get; set; }

        public bool IsPlaying
        {
            get
            {
                return this._IsPlaying;
            }
            set
            {
                this._IsPlaying = value;
                this.OnIsPlayingChanged();
            }
        }

        protected virtual void OnIsPlayingChanged()
        {
            if (this.IsPlayingChanged != null)
            {
                this.IsPlayingChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsPlaying");
        }

        public event EventHandler IsPlayingChanged;

        public ICommand Command
        {
            get
            {
                return CommandFactory.Instance.CreateCommand(
                    () =>
                    {
                        if (this.PlaybackManager.CurrentStream == null)
                        {
                            return this.PlaylistManager.Next();
                        }
                        else if (this.PlaybackManager.CurrentStream.IsPaused)
                        {
                            return this.PlaybackManager.CurrentStream.Resume();
                        }
                        else if (this.PlaybackManager.CurrentStream.IsStopped)
                        {
                            return this.PlaybackManager.CurrentStream.Play();
                        }
                        else
                        {
                            return this.PlaybackManager.CurrentStream.Pause();
                        }
                    }
                );
            }
        }

        protected override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaybackManager = core.Managers.Playback;
            base.InitializeComponent(core);
        }

        protected virtual void OnNotify(object sender, EventArgs e)
        {
            var isPlaying = default(bool);
            if (this.PlaybackManager != null)
            {
                var currentStream = this.PlaybackManager.CurrentStream;
                if (currentStream != null)
                {
                    isPlaying = currentStream.IsPlaying;
                }
            }
            if (this.IsPlaying != isPlaying)
            {
                this.IsPlaying = isPlaying;
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Play();
        }

        protected override void OnDisposing()
        {
            PlaybackStateNotifier.Notify -= this.OnNotify;
            base.OnDisposing();
        }
    }
}
