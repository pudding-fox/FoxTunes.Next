using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    public class PeakEQ : BaseComponent, IDisposable
    {
        public const float MIN_BANDWIDTH = 0.5f;

        public const float MAX_BANDWIDTH = 5.0f;

        public const float MIN_GAIN = -15;

        public const float MAX_GAIN = 15;

        public PeakEQ(int channelHandle)
        {
            this.Effect = new PeakEQEffect(channelHandle);
        }

        public PeakEQEffect Effect { get; private set; }

        public void UpdateBand(int band, float bandwidth, float center, float gain)
        {
            this.Effect.Band = band;
            this.Effect.Center = center;
            this.Effect.Channel = -1;
            this.Effect.Bandwidth = bandwidth;
            this.Effect.Gain = gain;
            this.Effect.Activate();
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
            if (this.Effect != null)
            {
                this.Effect.Dispose();
            }
        }

        ~PeakEQ()
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
