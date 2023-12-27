using FoxTunes.Interfaces;
using ManagedBass;
using System;

namespace FoxTunes
{
    public abstract class BassStreamComponent : BaseComponent, IBassStreamComponent
    {
        protected BassStreamComponent(IBassStreamPipeline pipeline, BassFlags flags)
        {
            this.Pipeline = pipeline;
            this.Flags = flags;
        }

        public abstract string Name { get; }

        public abstract string Description { get; }

        public abstract int ChannelHandle { get; protected set; }

        public IBassStreamPipeline Pipeline { get; private set; }

        public BassFlags Flags { get; protected set; }

        public virtual int BufferLength
        {
            get
            {
                return 0;
            }
        }

        public abstract bool IsActive { get; }

        public virtual bool IsStarting { get; set; }

        public virtual bool IsStopping { get; set; }

        public virtual bool GetFormat(out int rate, out int channels, out BassFlags flags)
        {
            return this.GetFormat(this.ChannelHandle, out rate, out channels, out flags);
        }

        public virtual bool GetFormat(int channelHandle, out int rate, out int channels, out BassFlags flags)
        {
            var info = default(ChannelInfo);
            if (!Bass.ChannelGetInfo(channelHandle, out info))
            {
                rate = 0;
                channels = 0;
                flags = this.Flags;
                return false;
            }
            channels = info.Channels;
            rate = info.Frequency;
            flags = info.Flags | this.Flags;
            return true;
        }

        public abstract void Connect(IBassStreamComponent previous);

        public virtual void ClearBuffer()
        {
            //Nothing to do.
        }

        protected virtual void OnInvalidate()
        {
            if (this.Invalidate == null)
            {
                return;
            }
            this.Invalidate(this, EventArgs.Empty);
        }

        public event EventHandler Invalidate;

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

        ~BassStreamComponent()
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
