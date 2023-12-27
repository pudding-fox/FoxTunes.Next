using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Crossfade;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace FoxTunes
{
    public class BassCrossfadeStreamInput : BassStreamInput, IBassStreamControllable
    {
        public BassCrossfadeStreamInput(BassCrossfadeStreamInputBehaviour behaviour, IBassStreamPipeline pipeline, BassFlags flags) : base(pipeline, flags)
        {
            this.Behaviour = behaviour;
        }

        public override string Name
        {
            get
            {
                return Strings.BassCrossfadeStreamInput_Name;
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
                    "{0} ({1:0.00s}/{2:0.00s}) ({3}/{4}/{5})",
                    this.Name,
                    (float)this.Behaviour.InPeriod / 1000,
                    (float)this.Behaviour.OutPeriod / 1000,
                    BassUtils.DepthDescription(flags),
                    MetaDataInfo.SampleRateDescription(rate),
                    MetaDataInfo.ChannelDescription(channels)
                );
            }
        }

        public BassCrossfadeStreamInputBehaviour Behaviour { get; private set; }

        public override int ChannelHandle { get; protected set; }

        public override int BufferLength
        {
            get
            {
                return BassUtils.GetMixerBufferLength();
            }
        }

        public override IEnumerable<int> Queue
        {
            get
            {
                var count = default(int);
                return BassCrossfade.GetChannels(out count);
            }
        }

        public override bool PreserveBuffer
        {
            get
            {
                //Disable clearing the buffer when the current track is removed as this would prevent fade out.
                return true;
            }
        }

        public override void Connect(BassOutputStream stream)
        {
            Logger.Write(this, LogLevel.Debug, "Creating BASS CROSSFADE stream with rate {0} and {1} channels.", stream.Rate, stream.Channels);
            this.ChannelHandle = BassCrossfade.StreamCreate(stream.Rate, stream.Channels, stream.Flags | BassFlags.MixerNonStop);
            if (this.ChannelHandle == 0)
            {
                BassUtils.Throw();
            }
        }

        protected override bool OnCheckFormat(BassOutputStream stream)
        {
            if (BassUtils.GetChannelDsdRaw(stream.ChannelHandle))
            {
                return false;
            }
            return base.OnCheckFormat(stream);
        }

        public override bool Contains(BassOutputStream stream)
        {
            return this.Queue.Contains(stream.ChannelHandle);
        }

        public override int Position(BassOutputStream stream)
        {
            var count = default(int);
            var channelHandles = BassCrossfade.GetChannels(out count);
            return channelHandles.IndexOf(stream.ChannelHandle);
        }

        public override bool Add(BassOutputStream stream, Action<BassOutputStream> callBack)
        {
            if (this.Queue.Contains(stream.ChannelHandle))
            {
                Logger.Write(this, LogLevel.Debug, "Stream is already enqueued: {0}", stream.ChannelHandle);
                return false;
            }
            Logger.Write(this, LogLevel.Debug, "Adding stream to the queue: {0}", stream.ChannelHandle);
            var flags = BassCrossfadeFlags.Default;
            if (this.IsStarting && this.Behaviour.Start)
            {
                Logger.Write(this, LogLevel.Debug, "Fading in...");
                flags = BassCrossfadeFlags.FadeIn;
            }
            this.Dispatch(() =>
            {
                BassUtils.OK(BassCrossfade.ChannelEnqueue(stream.ChannelHandle, flags));
                if (callBack != null)
                {
                    callBack(stream);
                }
            });
            return true;
        }

        public override bool Remove(BassOutputStream stream, Action<BassOutputStream> callBack)
        {
            if (!this.Queue.Contains(stream.ChannelHandle))
            {
                Logger.Write(this, LogLevel.Debug, "Stream is not enqueued: {0}", stream.ChannelHandle);
                return false;
            }
            Logger.Write(this, LogLevel.Debug, "Removing stream from the queue: {0}", stream.ChannelHandle);
            var flags = BassCrossfadeFlags.Default;
            if (this.Pipeline.Output.IsPlaying)
            {
                if (this.IsStopping && this.Behaviour.Stop)
                {
                    Logger.Write(this, LogLevel.Debug, "Fading out...");
                    flags = BassCrossfadeFlags.FadeOut;
                }
            }
            else
            {
                flags = BassCrossfadeFlags.None;
            }
            this.Dispatch(() =>
            {
                BassUtils.OK(BassCrossfade.ChannelRemove(stream.ChannelHandle, flags));
                if (callBack != null)
                {
                    callBack(stream);
                }
            });
            return true;
        }

        public override void ClearBuffer()
        {
            Logger.Write(this, LogLevel.Debug, "Clearing mixer buffer.");
            Bass.ChannelSetPosition(this.ChannelHandle, 0);
            base.ClearBuffer();
        }

        protected virtual void WaitForBuffer()
        {
            var bufferLength = this.Pipeline.BufferLength;
            if (bufferLength > 0)
            {
                Logger.Write(this, LogLevel.Debug, "Buffer length is {0}ms, waiting..", bufferLength);
                Thread.Sleep(bufferLength);
            }
        }

        public override void Reset()
        {
            Logger.Write(this, LogLevel.Debug, "Resetting the queue.");
            foreach (var channelHandle in this.Queue)
            {
                Logger.Write(this, LogLevel.Debug, "Removing stream from the queue: {0}", channelHandle);
                var flags = BassCrossfadeFlags.Default;
                if (this.Pipeline.Output.IsPlaying)
                {
                    if (this.IsStopping && this.Behaviour.Stop)
                    {
                        Logger.Write(this, LogLevel.Debug, "Fading out...");
                        flags = BassCrossfadeFlags.FadeOut;
                    }
                }
                else
                {
                    flags = BassCrossfadeFlags.None;
                }
                BassCrossfade.ChannelRemove(channelHandle, flags);
                if (flags == BassCrossfadeFlags.FadeOut)
                {
                    this.WaitForBuffer();
                }
            }
        }

        protected override void OnDisposing()
        {
            if (this.ChannelHandle != 0)
            {
                this.Reset();
                Logger.Write(this, LogLevel.Debug, "Freeing BASS CROSSFADE stream: {0}", this.ChannelHandle);
                Bass.StreamFree(this.ChannelHandle); //Not checking result code as it contains an error if the application is shutting down.
            }
        }

        #region IBassStreamControllable

        public void PreviewPlay()
        {
            //Nothing to do.
        }

        private bool Fading { get; set; }

        public void PreviewPause()
        {
            if (this.Behaviour.PauseResume)
            {
                Logger.Write(this, LogLevel.Debug, "Fading out...");
                this.Fading = true;
                BassCrossfade.StreamFadeOut();
                this.WaitForBuffer();
            }
        }

        public void PreviewResume()
        {
            if (this.Fading || this.Behaviour.PauseResume)
            {
                Logger.Write(this, LogLevel.Debug, "Fading in...");
                this.Fading = false;
                BassCrossfade.StreamFadeIn();
            }
        }

        public void PreviewStop()
        {
            //Nothing to do.
        }

        public void Play()
        {
            //Nothing to do.
        }

        public void Pause()
        {
            //Nothing to do.
        }

        public void Resume()
        {
            //Nothing to do.
        }

        public void Stop()
        {
            //Nothing to do.
        }

        #endregion

        public static bool CanCreate(BassCrossfadeStreamInputBehaviour behaviour, BassOutputStream stream, IBassStreamPipelineQueryResult query)
        {
            if (BassUtils.GetChannelDsdRaw(stream.ChannelHandle))
            {
                return false;
            }
            return true;
        }
    }
}
