using FoxTunes.Interfaces;
using ManagedBass;
using System;

namespace FoxTunes
{
    public class VolumeEffect : BaseComponent, IDisposable
    {
        private VolumeEffect()
        {
            this.Parameters = new VolumeParameters();
        }

        public VolumeEffect(int channelHandle) : this()
        {
            this.ChannelHandle = channelHandle;
        }

        public int EffectHandle { get; private set; }

        public bool IsActive { get; private set; }

        public VolumeParameters Parameters { get; private set; }

        public int ChannelHandle { get; private set; }

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

        public float Volume
        {
            get
            {
                return this.Parameters.fVolume;
            }
            set
            {
                this.Parameters.fVolume = value;
                this.OnPropertyChanged("Volume");
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

        ~VolumeEffect()
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
