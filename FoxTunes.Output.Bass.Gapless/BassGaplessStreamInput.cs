using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Gapless;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class BassGaplessStreamInput : BassStreamInput
    {
        public BassGaplessStreamInput(BassGaplessStreamInputBehaviour behaviour, IBassStreamPipeline pipeline, BassFlags flags) : base(pipeline, flags)
        {
            this.Behaviour = behaviour;
        }

        public override string Name
        {
            get
            {
                return "Gapless";
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

        public BassGaplessStreamInputBehaviour Behaviour { get; private set; }

        public override int ChannelHandle { get; protected set; }

        public override IEnumerable<int> Queue
        {
            get
            {
                var count = default(int);
                return BassGapless.GetChannels(out count);
            }
        }

        public override void Connect(BassOutputStream stream)
        {
            BassUtils.OK(BassGapless.SetConfig(BassGaplessAttriubute.KeepAlive, true));
            BassUtils.OK(BassGapless.SetConfig(BassGaplessAttriubute.RecycleStream, true));
            Logger.Write(this, LogLevel.Debug, "Creating BASS GAPLESS stream with rate {0} and {1} channels.", stream.Rate, stream.Channels);
            this.ChannelHandle = BassGapless.StreamCreate(stream.Rate, stream.Channels, stream.Flags | BassFlags.MixerNonStop);
            if (this.ChannelHandle == 0)
            {
                BassUtils.Throw();
            }
        }

        public override bool Contains(BassOutputStream stream)
        {
            return this.Queue.Contains(stream.ChannelHandle);
        }

        public override int Position(BassOutputStream stream)
        {
            var count = default(int);
            var channelHandles = BassGapless.GetChannels(out count);
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
            BassUtils.OK(BassGapless.ChannelEnqueue(stream.ChannelHandle));
            if (callBack != null)
            {
                callBack(stream);
            }
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
            BassUtils.OK(BassGapless.ChannelRemove(stream.ChannelHandle));
            if (callBack != null)
            {
                callBack(stream);
            }
            return true;
        }

        public override void Reset()
        {
            Logger.Write(this, LogLevel.Debug, "Resetting the queue.");
            foreach (var channelHandle in this.Queue)
            {
                Logger.Write(this, LogLevel.Debug, "Removing stream from the queue: {0}", channelHandle);
                BassGapless.ChannelRemove(channelHandle);
            }
        }

        protected override void OnDisposing()
        {
            if (this.ChannelHandle != 0)
            {
                this.Reset();
                Logger.Write(this, LogLevel.Debug, "Freeing BASS GAPLESS stream: {0}", this.ChannelHandle);
                Bass.StreamFree(this.ChannelHandle); //Not checking result code as it contains an error if the application is shutting down.
            }
        }
    }
}
