using FoxTunes.Interfaces;
using System;
using System.Timers;

namespace FoxTunes
{
    public class ChannelMonitor : BaseComponent, IDisposable
    {
        public static readonly int INTERVAL = 500;

        public static readonly object SyncRoot = new object();

        public ChannelMonitor(MoodBarItem moodBarItem, IBassStream stream)
        {
            this.MoodBarItem = moodBarItem;
            this.Stream = stream;
        }

        public MoodBarItem MoodBarItem { get; private set; }

        public IBassStream Stream { get; private set; }

        public global::System.Timers.Timer Timer { get; private set; }

        public void Start()
        {
            lock (SyncRoot)
            {
                if (this.Timer == null)
                {
                    this.Timer = new global::System.Timers.Timer();
                    this.Timer.Interval = INTERVAL;
                    this.Timer.AutoReset = false;
                    this.Timer.Elapsed += this.OnElapsed;
                    this.Timer.Start();
                }
            }
        }

        public void Stop()
        {
            lock (SyncRoot)
            {
                if (this.Timer != null)
                {
                    this.Timer.Stop();
                    this.Timer.Elapsed -= this.OnElapsed;
                    this.Timer.Dispose();
                    this.Timer = null;
                }
            }
        }

        protected virtual void Update()
        {
            var length = this.Stream.Length;
            var position = this.Stream.Position;
            if (position == length)
            {
                this.MoodBarItem.Progress = MoodBarItem.PROGRESS_COMPLETE;
                this.Stop();
            }
            else
            {
                this.MoodBarItem.Progress = (int)(((float)position / length) * 100);
                this.MoodBarItem.Status = MoodBarItemStatus.Processing;
            }
        }

        protected virtual void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                this.Update();
                lock (SyncRoot)
                {
                    if (this.Timer == null)
                    {
                        return;
                    }
                    this.Timer.Start();
                }
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            this.Stop();
        }

        ~ChannelMonitor()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
