using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Mix;
using ManagedBass.Wasapi;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class BassWasapiStreamOutput : BassStreamOutput
    {
        private BassWasapiStreamOutput(IBassStreamPipeline pipeline, BassFlags flags) : base(pipeline, flags)
        {
            this.MixerChannelHandles = new HashSet<int>();
        }

        public BassWasapiStreamOutput(BassWasapiStreamOutputBehaviour behaviour, IBassStreamPipeline pipeline, BassFlags flags) : this(pipeline, flags)
        {
            this.Behaviour = behaviour;
        }

        public override string Name
        {
            get
            {
                return Strings.WASAPI;
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

        public BassWasapiStreamOutputBehaviour Behaviour { get; private set; }

        public override int ChannelHandle { get; protected set; }

        public override int BufferLength
        {
            get
            {
                var bufferLength = Convert.ToInt32(Bass.ChannelBytes2Seconds(this.ChannelHandle, BassWasapi.Info.BufferLength) * 1000);
                foreach (var channelHandle in this.MixerChannelHandles)
                {
                    bufferLength += BassUtils.GetMixerBufferLength();
                }
                return bufferLength;
            }
        }

        protected override IEnumerable<int> GetMixerChannelHandles()
        {
            return this.MixerChannelHandles;
        }

        public override bool CheckFormat(int rate, int channels)
        {
            return BassWasapiDevice.Info.SupportedRates.Contains(rate) && channels <= BassWasapiDevice.Info.Outputs;
        }

        public override void Connect(IBassStreamComponent previous)
        {
            var rate = default(int);
            var channels = default(int);
            var flags = default(BassFlags);
            this.ConfigureWASAPI(previous, out rate, out channels, out flags);
            if (this.ShouldCreateMixer(previous, rate, channels, flags))
            {
                Logger.Write(this, LogLevel.Debug, "Creating BASS MIX stream with rate {0} and {1} channels.", rate, channels);
                this.ChannelHandle = BassMix.CreateMixerStream(rate, channels, flags);
                if (this.ChannelHandle == 0)
                {
                    BassUtils.Throw();
                }
                Logger.Write(this, LogLevel.Debug, "Adding stream to the mixer: {0}", previous.ChannelHandle);
                BassUtils.OK(BassMix.MixerAddChannel(this.ChannelHandle, previous.ChannelHandle, BassFlags.Default | BassFlags.MixerBuffer | BassFlags.MixerDownMix));
                BassUtils.OK(BassWasapiHandler.StreamSet(this.ChannelHandle));
                this.MixerChannelHandles.Add(previous.ChannelHandle);
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "The stream properties match the device, playing directly.");
                BassUtils.OK(BassWasapiHandler.StreamSet(previous.ChannelHandle));
                this.ChannelHandle = previous.ChannelHandle;
            }
            this.UpdateVolume();
        }

        protected virtual void ConfigureWASAPI(IBassStreamComponent previous, out int rate, out int channels, out BassFlags flags)
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
                    if (!BassWasapiDevice.Info.SupportedRates.Contains(targetRate))
                    {
                        var nearestRate = BassWasapiDevice.Info.GetNearestRate(targetRate);
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
                    if (!BassWasapiDevice.Info.SupportedRates.Contains(rate))
                    {
                        var nearestRate = BassWasapiDevice.Info.GetNearestRate(rate);
                        Logger.Write(this, LogLevel.Debug, "Stream rate {0} isn't supposed by the device, falling back to {1}.", rate, nearestRate);
                        rate = nearestRate;
                    }
                }
                if (BassWasapiDevice.Info.Outputs < channels)
                {
                    Logger.Write(this, LogLevel.Debug, "Stream channel count {0} isn't supported by the device, falling back to {1} channels.", channels, BassWasapiDevice.Info.Outputs);
                    channels = BassWasapiDevice.Info.Outputs;
                }
            }
            BassWasapiDevice.Init(
                this.Behaviour.WasapiDevice,
                this.Behaviour.Exclusive,
                this.Behaviour.AutoFormat,
                this.Behaviour.BufferLength,
                this.Behaviour.DoubleBuffer,
                this.Behaviour.EventDriven,
                this.Behaviour.Async,
                this.Behaviour.Dither,
                this.Behaviour.Raw,
                rate,
                channels
            );
        }

        protected virtual bool StartWASAPI()
        {
            if (BassWasapi.IsStarted)
            {
                Logger.Write(this, LogLevel.Debug, "WASAPI has already been started.");
                return false;
            }
            Logger.Write(this, LogLevel.Debug, "Starting WASAPI.");
            BassUtils.OK(BassWasapi.Start());
            return true;
        }

        protected virtual bool StopWASAPI(bool reset)
        {
            if (!BassWasapi.IsStarted)
            {
                Logger.Write(this, LogLevel.Debug, "WASAPI has not been started.");
                return false;
            }
            Logger.Write(this, LogLevel.Debug, "Stopping WASAPI.");
            BassUtils.OK(BassWasapi.Stop(reset));
            return true;
        }

        protected virtual bool ShouldCreateMixer(IBassStreamComponent previous, int rate, int channels, BassFlags flags)
        {
            if (flags.HasFlag(BassFlags.DSDRaw))
            {
                //Can't create mixer for DSD.
                return false;
            }
            if (this.Behaviour.Mixer)
            {
                //Mixer is forced on, probably so visualizations work.
                return true;
            }
            var _rate = default(int);
            var _channels = default(int);
            var _flags = default(BassFlags);
            previous.GetFormat(out _rate, out _channels, out _flags);
            if (rate != _rate || channels != _channels)
            {
                //Stream rate or channel count differs from device.
                return true;
            }
            return false;
        }

        public override void ClearBuffer()
        {
            foreach (var channelHandle in this.MixerChannelHandles)
            {
                Logger.Write(this, LogLevel.Debug, "Clearing mixer buffer.");
                Bass.ChannelSetPosition(channelHandle, 0);
            }
            base.ClearBuffer();
        }

        public override bool IsPlaying
        {
            get
            {
                return BassWasapi.IsStarted;
            }
        }

        private bool _IsPaused { get; set; }

        public override bool IsPaused
        {
            get
            {
                return this._IsPaused;
            }
        }

        public override bool IsStopped
        {
            get
            {
                return !BassWasapi.IsStarted;
            }
        }

        public override void Play()
        {
            if (this.IsPlaying)
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Starting WASAPI.");
            try
            {
                BassUtils.OK(this.StartWASAPI());
                this._IsPaused = false;
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
            Logger.Write(this, LogLevel.Debug, "Pausing WASAPI.");
            try
            {
                BassUtils.OK(this.StopWASAPI(false));
                this._IsPaused = true;
            }
            catch (Exception e)
            {
                this.ErrorEmitter.Send(this, e);
                throw;
            }
        }

        public override void Resume()
        {
            this.Play();
        }

        public override void Stop()
        {
            if (this.IsStopped)
            {
                return;
            }
            try
            {
                BassUtils.OK(this.StopWASAPI(true));
                this._IsPaused = false;
            }
            catch (Exception e)
            {
                this.ErrorEmitter.Send(this, e);
                throw;
            }
        }

        protected override float GetVolume()
        {
            if (this.Behaviour.Exclusive)
            {
                return 1;
            }
            return BassWasapi.GetVolume(WasapiVolumeTypes.Session);
        }

        protected override void SetVolume(float volume)
        {
            if (this.Behaviour.Exclusive)
            {
                if (volume != 1)
                {
                    this.ErrorEmitter.Send(this, Strings.BassWasapiStreamOutput_VolumeExclusive);
                }
                return;
            }
            if (!BassWasapi.SetVolume(WasapiVolumeTypes.Session, volume))
            {
                //TODO: Warn.
            }
        }

        protected override void OnDisposing()
        {
            foreach (var channelHandle in this.MixerChannelHandles)
            {
                Logger.Write(this, LogLevel.Debug, "Freeing BASS stream: {0}", channelHandle);
                Bass.StreamFree(channelHandle); //Not checking result code as it contains an error if the application is shutting down.
            }
            this.Stop();
            BassWasapiDevice.Free();
            base.OnDisposing();
        }
    }
}
