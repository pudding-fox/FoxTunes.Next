using FoxTunes.Interfaces;
using ManagedBass;
using System;

namespace FoxTunes
{
    public class PeakEQEffect : BaseComponent, IDisposable
    {
        private PeakEQEffect()
        {
            this.Parameters = new PeakEQParameters();
        }

        public PeakEQEffect(int channelHandle) : this()
        {
            this.ChannelHandle = channelHandle;
        }

        public int EffectHandle { get; private set; }

        public bool IsActive { get; private set; }

        public PeakEQParameters Parameters { get; private set; }

        public int ChannelHandle { get; private set; }

        public int Band
        {
            get
            {
                return this.Parameters.lBand;
            }
            set
            {
                this.Parameters.lBand = value;
                this.OnPropertyChanged("Band");
            }
        }

        public float Bandwidth
        {
            get
            {
                return this.Parameters.fBandwidth;
            }
            set
            {
                this.Parameters.fBandwidth = value;
                this.OnPropertyChanged("Bandwidth");
            }
        }

        public float Q
        {
            get
            {
                return this.Parameters.fQ;
            }
            set
            {
                this.Parameters.fQ = value;
                this.OnPropertyChanged("Q");
            }
        }

        public float Center
        {
            get
            {
                return this.Parameters.fCenter;
            }
            set
            {
                this.Parameters.fCenter = value;
                this.OnPropertyChanged("Center");
            }
        }

        public float Gain
        {
            get
            {
                return this.Parameters.fGain;
            }
            set
            {
                this.Parameters.fGain = value;
                this.OnPropertyChanged("Gain");
            }
        }

        public int Channel
        {
            get
            {
                return this.Parameters.lChannel;
            }
            set
            {
                this.Parameters.lChannel = value;
                this.OnPropertyChanged("Channel");
            }
        }

        public void Activate()
        {
            if (!this.IsActive)
            {
                this.EffectHandle = Bass.ChannelSetFX(this.ChannelHandle, this.Parameters.FXType, 0);
                if (this.EffectHandle == 0)
                {
                    BassUtils.Throw();
                }
                this.IsActive = true;
            }
            if (!Bass.FXSetParameters(this.EffectHandle, this.Parameters))
            {
                BassUtils.Throw();
            }
        }

        public void Deactivate()
        {
            if (!this.IsActive)
            {
                return;
            }
            //Ignoring result on purpose.
            Bass.ChannelRemoveFX(this.ChannelHandle, this.EffectHandle);
            this.EffectHandle = 0;
            this.IsActive = false;
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
            this.Deactivate();
        }

        ~PeakEQEffect()
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
