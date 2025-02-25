using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Mix;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class BassDirectSoundStreamOutput : BassStreamOutput
    {
        private BassDirectSoundStreamOutput(IBassStreamPipeline pipeline, BassFlags flags) : base(pipeline, flags)
        {
            this.MixerChannelHandles = new HashSet<int>();
        }

        public BassDirectSoundStreamOutput(BassDirectSoundStreamOutputBehaviour behaviour, IBassStreamPipeline pipeline, BassFlags flags) : this(pipeline, flags)
        {
            this.Behaviour = behaviour;
        }

        public override string Name
        {
            get
            {
                return Strings.DirectSound;
            }
        }

        public override string Description
        {
            get
            {
                var rate = default(int);
                var channels = default(int);
                var flags = default(BassFlags);
                if (!this.GetFormat(out rate, out channels, out flags))
                {
                    rate = 0;
                    channels = 0;
                    flags = BassFlags.Default;
                }
                return string.Format(
                    "{0} ({1}/{2}/{3})",
                    this.Name,
                    BassUtils.DepthDescription(flags),
                    MetaDataInfo.SampleRateDescription(rate),
                    MetaDataInfo.ChannelDescription(channels)
                );
            }
        }

        public HashSet<int> MixerChannelHandles { get; protected set; }

        public BassDirectSoundStreamOutputBehaviour Behaviour { get; private set; }

        public override int ChannelHandle { get; protected set; }

        public override int BufferLength
        {
            get
            {
                return BassUtils.GetMixerBufferLength() + Bass.UpdatePeriod;
            }
        }

        protected override IEnumerable<int> GetMixerChannelHandles()
        {
            return this.MixerChannelHandles;
        }

        public override bool CheckFormat(int rate, int channels)
        {
            return true;
        }

        public override void Connect(IBassStreamComponent previous)
        {
            var rate = default(int);
            var channels = default(int);
            var flags = default(BassFlags);
            this.ConfigureDirectSound(previous, out rate, out channels, out flags);
            Logger.Write(this, LogLevel.Debug, "Creating BASS MIX stream with rate {0} and {1} channels.", rate, channels);
            this.ChannelHandle = BassMix.CreateMixerStream(rate, channels, flags);
            if (this.ChannelHandle == 0)
            {
                BassUtils.Throw();
            }
            Logger.Write(this, LogLevel.Debug, "Adding stream to the mixer: {0}", previous.ChannelHandle);
            BassUtils.OK(BassMix.MixerAddChannel(this.ChannelHandle, previous.ChannelHandle, BassFlags.Default | BassFlags.MixerBuffer | BassFlags.MixerDownMix));
            this.MixerChannelHandles.Add(previous.ChannelHandle);
            this.UpdateVolume();
        }

        protected virtual void ConfigureDirectSound(IBassStreamComponent previous, out int rate, out int channels, out BassFlags flags)
        {
            previous.GetFormat(out rate, out channels, out flags);
            if (flags.HasFlag(BassFlags.DSDRaw))
            {
                //Nothing to do.
            }
            else
            {
                if (this.Behaviour.Output.EnforceRate)
                {
                    var targetRate = this.Behaviour.Output.Rate;
                    if (!BassDirectSoundDevice.Info.SupportedRates.Contains(targetRate))
                    {
                        var nearestRate = BassDirectSoundDevice.Info.GetNearestRate(targetRate);
                        Logger.Write(this, LogLevel.Warn, "Enforced rate {0} isn't supposed by the device, falling back to {1}.", targetRate, nearestRate);
                        rate = nearestRate;
                    }
                    else
                    {
                        rate = targetRate;
                    }
                }
                else
                {
                    if (!BassDirectSoundDevice.Info.SupportedRates.Contains(rate))
                    {
                        var nearestRate = BassDirectSoundDevice.Info.GetNearestRate(rate);
                        Logger.Write(this, LogLevel.Debug, "Stream rate {0} isn't supposed by the device, falling back to {1}.", rate, nearestRate);
                        rate = nearestRate;
                    }
                }
                if (BassDirectSoundDevice.Info.Outputs < channels)
                {
                    Logger.Write(this, LogLevel.Debug, "Stream channel count {0} isn't supported by the device, falling back to {1} channels.", channels, BassDirectSoundDevice.Info.Outputs);
                    channels = BassDirectSoundDevice.Info.Outputs;
                }
            }
            flags = flags & ~BassFlags.Decode;
        }

        public override void ClearBuffer()
        {
            Logger.Write(this, LogLevel.Debug, "Clearing mixer buffer.");
            Bass.ChannelSetPosition(this.ChannelHandle, 0);
            base.ClearBuffer();
        }

        public override bool IsPlaying
        {
            get
            {
                var state = Bass.ChannelIsActive(this.ChannelHandle);
                return state == PlaybackState.Playing || state == PlaybackState.Stalled;
            }
        }

        public override bool IsPaused
        {
            get
            {
                return Bass.ChannelIsActive(this.ChannelHandle) == PlaybackState.Paused;
            }
        }

        public override bool IsStopped
        {
            get
            {
                return Bass.ChannelIsActive(this.ChannelHandle) == PlaybackState.Stopped;
            }
        }

        public override void Play()
        {
            if (this.IsPlaying)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Playing channel: {0}", this.ChannelHandle);
            try
            {
                BassUtils.OK(Bass.ChannelPlay(this.ChannelHandle, true));
            }
            catch (Exception e)
            {
                this.ErrorEmitter.Send(this, e);
                throw;
            }
        }

        public override void Pause()
        {
            if (!this.IsPlaying)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Pausing channel: {0}", this.ChannelHandle);
            try
            {
                BassUtils.OK(Bass.ChannelPause(this.ChannelHandle));
            }
            catch (Exception e)
            {
                this.ErrorEmitter.Send(this, e);
                throw;
            }
        }

        public override void Resume()
        {
            if (this.IsPlaying)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Resuming channel: {0}", this.ChannelHandle);
            try
            {
                BassUtils.OK(Bass.ChannelPlay(this.ChannelHandle, false));
            }
            catch (Exception e)
            {
                this.ErrorEmitter.Send(this, e);
                throw;
            }
        }

        public override void Stop()
        {
            if (this.IsStopped)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Stopping channel: {0}", this.ChannelHandle);
            try
            {
                BassUtils.OK(Bass.ChannelStop(this.ChannelHandle));
            }
            catch (Exception e)
            {
                this.ErrorEmitter.Send(this, e);
                throw;
            }
        }

        protected override float GetVolume()
        {
            if (this.ChannelHandle == 0)
            {
                return 0;
            }
            var volume = default(float);
            if (!Bass.ChannelGetAttribute(this.ChannelHandle, ChannelAttribute.Volume, out volume))
            {
                //TODO: Warn.
                return 0;
            }
            return volume;
        }

        protected override void SetVolume(float volume)
        {
            if (this.ChannelHandle == 0)
            {
                return;
            }
            if (!Bass.ChannelSetAttribute(this.ChannelHandle, ChannelAttribute.Volume, volume))
            {
                //TODO: Warn.
            }
        }

        protected override void OnDisposing()
        {
            if (this.ChannelHandle != 0)
            {
                Logger.Write(this, LogLevel.Debug, "Freeing BASS stream: {0}", this.ChannelHandle);
                Bass.StreamFree(this.ChannelHandle); //Not checking result code as it contains an error if the application is shutting down.
            }
            base.OnDisposing();
        }
    }
}
