using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class UnloadOutputStreamTask : BackgroundTask
    {
        public const string ID = "E6D893E8-EC80-4823-A66C-286DB69A48D1";

        public UnloadOutputStreamTask(IOutputStream outputStream)
            : base(ID)
        {
            this.OutputStream = outputStream;
        }

        public IOutputStream OutputStream { get; private set; }

        public IOutput Output { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output;
            this.PlaybackManager = core.Managers.Playback;
            base.InitializeComponent(core);
        }

        protected override Task OnRun()
        {
            if (this.OutputStream != null && !this.OutputStream.IsDisposed)
            {
                Logger.Write(this, LogLevel.Debug, "Unloading output stream: {0} => {1}", this.OutputStream.Id, this.OutputStream.FileName);
                return this.Output.Unload(this.OutputStream);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
