using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    public class MoodBarRenderer : MoodBarRendererBase
    {
        public IPlaybackManager PlaybackManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            if (this.PlaybackManager.CurrentStream != null)
            {
                var task = Windows.Invoke(() => this.FileData = this.PlaybackManager.CurrentStream.PlaylistItem);
            }
            else
            {
                var task = Windows.Invoke(() => this.FileData = null);
            }
        }

        protected override void OnDisposing()
        {
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            }
            base.OnDisposing();
        }
    }
}
