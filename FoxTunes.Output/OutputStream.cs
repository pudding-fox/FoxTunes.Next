using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class OutputStream : BaseComponent, IOutputStream
    {
        protected OutputStream(PlaylistItem playlistItem)
        {
            this.PlaylistItem = playlistItem;
        }

        public int Id
        {
            get
            {
                return this.PlaylistItem.Id;
            }
        }

        public string FileName
        {
            get
            {
                return this.PlaylistItem.FileName;
            }
        }

        public PlaylistItem PlaylistItem { get; private set; }

        public abstract long Position { get; }

        public abstract long ActualPosition { get; }

        public abstract long Length { get; }

        public abstract int Rate { get; }

        public abstract int Channels { get; }

        public abstract bool IsReady { get; }

        public abstract bool IsPlaying { get; }

        public abstract bool IsPaused { get; }

        public abstract bool IsStopped { get; }

        public abstract bool IsEnded { get; }

        public abstract Task Play();

        public abstract Task Pause();

        public abstract Task Resume();

        public abstract Task Stop();

        public abstract Task Seek(long position);

        public abstract event EventHandler Ending;

        public abstract event EventHandler Ended;

        public virtual Task BeginSeek()
        {
            if (!this.IsPlaying)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Pause();
        }

        public virtual Task EndSeek()
        {
            if (!this.IsPaused)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Resume();
        }

        public abstract TimeSpan GetDuration(long position);

        public abstract OutputStreamFormat Format { get; }

        public abstract T[] GetBuffer<T>(TimeSpan duration) where T : struct;

        public abstract int GetData(short[] buffer);

        public abstract int GetData(float[] buffer);

        public string Description
        {
            get
            {
                return string.Format("Length = {0},  Rate {1}, Channels = {2}", this.Length, this.Rate, this.Channels);
            }
        }

        public abstract bool CanReset { get; }

        public abstract void Reset();

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

        protected abstract void OnDisposing();

        ~OutputStream()
        {
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
