using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Gapless;
using System.Linq;
using System.Collections.Generic;
using System;
using ManagedBass.Cd;

namespace FoxTunes
{
    public class BassCdStreamInput : BassStreamInput
    {
        public BassCdStreamInput(BassCdBehaviour behaviour, int drive, IBassStreamPipeline pipeline, BassFlags flags) : base(pipeline, flags)
        {
            this.Behaviour = behaviour;
            this.Drive = drive;
        }

        public override IEnumerable<Type> SupportedProviders
        {
            get
            {
                return new[]
                {
                    typeof(BassCdStreamProvider)
                };
            }
        }

        public override string Name
        {
            get
            {
                return "CD";
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

        public BassCdBehaviour Behaviour { get; private set; }

        public int Drive { get; private set; }

        public override int ChannelHandle { get; protected set; }

        public override IEnumerable<int> Queue
        {
            get
            {
                var count = default(int);
                return BassGapless.GetChannels(out count);
            }
        }

        public bool SetTrack(int track, out int channelHandle)
        {
            channelHandle = this.Queue.FirstOrDefault();
            if (channelHandle == 0)
            {
                return false;
            }
            if (BassCd.StreamGetTrack(channelHandle) != track)
            {
                Logger.Write(this, LogLevel.Debug, "Switching CD track: {0}", track);
                BassUtils.OK(BassCd.StreamSetTrack(channelHandle, track));
            }
            return true;
        }

        public override void Connect(BassOutputStream stream)
        {
            BassUtils.OK(BassGapless.SetConfig(BassGaplessAttriubute.KeepAlive, true));
            BassUtils.OK(BassGapless.SetConfig(BassGaplessAttriubute.RecycleStream, true));
            BassUtils.OK(BassGapless.ChannelEnqueue(stream.ChannelHandle));
            BassUtils.OK(BassGapless.Cd.Enable(this.Drive, stream.Flags));
            Logger.Write(this, LogLevel.Debug, "Creating BASS GAPLESS stream with rate {0} and {1} channels.", stream.Rate, stream.Channels);
            this.ChannelHandle = BassGapless.StreamCreate(stream.Rate, stream.Channels, stream.Flags);
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
            return POSITION_INDETERMINATE;
        }

        public override bool Add(BassOutputStream stream, Action<BassOutputStream> callBack)
        {
            if (!this.Contains(stream))
            {
                //I think this happens when the CD has ended.
                BassUtils.OK(BassGapless.ChannelEnqueue(stream.ChannelHandle));
            }
            return false;
        }

        public override bool Remove(BassOutputStream stream, Action<BassOutputStream> callBack)
        {
            return false;
        }

        public override void Reset()
        {
            Logger.Write(this, LogLevel.Debug, "Resetting the queue.");
            foreach (var channelHandle in this.Queue)
            {
                Logger.Write(this, LogLevel.Debug, "Removing stream from the queue: {0}", channelHandle);
                BassGapless.ChannelRemove(channelHandle);
                Bass.StreamFree(channelHandle);
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
            BassGapless.Cd.Disable();
            BassCd.Release(this.Drive);
        }
    }
}
