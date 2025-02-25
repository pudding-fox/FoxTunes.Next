using FoxTunes.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class LoadOutputStreamTask : BackgroundTask
    {
        public const string ID = "E3E23677-DE0A-4291-8416-BC4A91856037";

        public LoadOutputStreamTask(PlaylistItem playlistItem, bool immediate)
            : base(ID)
        {
            this.PlaylistItem = playlistItem;
            this.Immediate = immediate;
        }

        public PlaylistItem PlaylistItem { get; private set; }

        public bool Immediate { get; private set; }

        public IOutput Output { get; private set; }

        public IOutputStreamQueue OutputStreamQueue { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public override bool Visible
        {
            get
            {
                if (this.Output != null && this.Output.ShowBuffering && this.Immediate && !this.OutputStreamQueue.IsQueued(this.PlaylistItem))
                {
                    return true;
                }
                return base.Visible;
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output;
            this.OutputStreamQueue = core.Components.OutputStreamQueue;
            this.SignalEmitter = core.Components.SignalEmitter;
            base.InitializeComponent(core);
        }

        protected override async Task OnStarted()
        {
            this.Name = "Buffering";
            this.Description = this.PlaylistItem.FileName.GetName();
            await base.OnStarted().ConfigureAwait(false);
        }

        protected override async Task OnRun()
        {
            if (this.OutputStreamQueue.IsQueued(this.PlaylistItem))
            {
                Logger.Write(this, LogLevel.Debug, "Play list item already exists in the queue:  {0} => {1}", this.PlaylistItem.Id, this.PlaylistItem.FileName);
                if (this.Immediate)
                {
                    Logger.Write(this, LogLevel.Debug, "Immediate load was requested, de-queuing: {0} => {1}", this.PlaylistItem.Id, this.PlaylistItem.FileName);
                    await this.OutputStreamQueue.Dequeue(this.PlaylistItem).ConfigureAwait(false);
                }
                return;
            }
            await this.CheckPath().ConfigureAwait(false);
            var outputStream = await this.Output.Load(this.PlaylistItem, this.Immediate).ConfigureAwait(false);
            if (outputStream == null)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to load play list item into output stream: {0} => {1}", this.PlaylistItem.Id, this.PlaylistItem.FileName);
                if (this.Immediate)
                {
                    throw new InvalidOperationException(string.Format("Failed to load stream: {0}", this.PlaylistItem.FileName));
                }
                else
                {
                    //This can happen when attempting to preemptively load the next track of something that doesn't allow concurrent streams, like a CD.
                    return;
                }
            }
            Logger.Write(this, LogLevel.Debug, "Play list item loaded into output stream: {0} => {1}", this.PlaylistItem.Id, this.PlaylistItem.FileName);
            await this.OutputStreamQueue.Enqueue(outputStream, this.Immediate).ConfigureAwait(false);
            if (this.Immediate)
            {
                Logger.Write(this, LogLevel.Debug, "Immediate load was requested, output stream was automatically de-queued: {0} => {1}", this.PlaylistItem.Id, this.PlaylistItem.FileName);
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Output stream added to the queue: {0} => {1}", this.PlaylistItem.Id, this.PlaylistItem.FileName);
            }
        }

        protected virtual async Task CheckPath()
        {
            if (!string.IsNullOrEmpty(Path.GetPathRoot(this.PlaylistItem.FileName)) && !File.Exists(this.PlaylistItem.FileName))
            {
                if (!NetworkDrive.IsRemotePath(this.PlaylistItem.FileName) || !await NetworkDrive.ConnectRemotePath(this.PlaylistItem.FileName).ConfigureAwait(false))
                {
                    throw new FileNotFoundException(string.Format("File not found: {0}", this.PlaylistItem.FileName), this.PlaylistItem.FileName);
                }
            }
        }
    }
}
