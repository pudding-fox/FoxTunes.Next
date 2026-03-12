using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Mix;

namespace FoxTunes
{
    public class BassUpmixStreamComponent : BassStreamComponent
    {
        public BassUpmixStreamComponent(BassUpmixStreamComponentBehaviour behaviour, IBassStreamPipeline pipeline, IBassStreamPipelineQueryResult query, BassFlags flags) : base(pipeline, flags)
        {
            this.Behaviour = behaviour;
            this.Query = query;
        }

        public override string Name
        {
            get
            {
                return Strings.BassUpmixStreamComponent_Name;
            }
        }

        public override string Description
        {
            get
            {
                return string.Format(
                    "{0} ({1} -> {2})",
                    this.Name,
                    MetaDataInfo.ChannelDescription(this.InputChannels),
                    MetaDataInfo.ChannelDescription(this.Query.OutputChannels)
                );
            }
        }

        public BassUpmixStreamComponentBehaviour Behaviour { get; private set; }

        public IBassStreamPipelineQueryResult Query { get; private set; }

        public int InputChannels { get; protected set; }

        public override int ChannelHandle { get; protected set; }

        public override int BufferLength
        {
            get
            {
                return BassMix.MixerBufferLength * Bass.PlaybackBufferLength;
            }
        }

        public override bool IsActive
        {
            get
            {
                return true;
            }
        }

        public override void Connect(IBassStreamComponent previous)
        {
            var rate = default(int);
            var channels = default(int);
            var flags = default(BassFlags);
            previous.GetFormat(out rate, out channels, out flags);
            Logger.Write(this, LogLevel.Debug, "Creating BASS MIX stream with channels {0} => {1}", channels, this.Query.OutputChannels);
            this.InputChannels = channels;
            this.ChannelHandle = BassMix.CreateMixerStream(rate, this.Query.OutputChannels, flags);
            if (this.ChannelHandle == 0)
            {
                BassUtils.Throw();
            }
            var matrix = GetMatrix(channels, this.Query.OutputChannels);
            BassUtils.OK(BassMix.MixerAddChannel(this.ChannelHandle, previous.ChannelHandle, BassFlags.Default | BassFlags.MixerMatrix));
            BassUtils.OK(BassMix.ChannelSetMatrix(previous.ChannelHandle, matrix));
        }

        protected override void OnDisposing()
        {
            if (this.ChannelHandle != 0)
            {
                Logger.Write(this, LogLevel.Debug, "Freeing BASS SOX stream: {0}", this.ChannelHandle);
                BassUtils.OK(Bass.StreamFree(this.ChannelHandle));
            }
        }

        public static bool ShouldCreate(BassUpmixStreamComponentBehaviour behaviour, BassOutputStream stream, IBassStreamPipelineQueryResult query)
        {
            if (BassUtils.GetChannelDsdRaw(stream.ChannelHandle))
            {
                //Cannot upmix DSD.
                return false;
            }
            if (stream.Channels < query.OutputChannels)
            {
                var matrix = GetMatrix(stream.Channels, query.OutputChannels);
                if (matrix != null)
                {
                    //More output channels than input channels and we have a matrix for it.
                    return true;
                }
            }
            //Something else.
            return false;
        }

        public static float[,] GetMatrix(int inputChannels, int outputChannels)
        {
            switch (inputChannels)
            {
                //We only implement stereo upmixing.
                case 2:
                    switch (outputChannels)
                    {
                        case 3:
                            return new float[,]
                            {
                                { 1, 0 },      //FL
                                { 0, 1 },      //FR
                                { 0.5f, 0.5f } //C
                            };
                        case 4:
                            return new float[,]
                            {
                                { 1, 0 }, //FL
                                { 0, 1 }, //FR
                                { 1, 0 }, //RL
                                { 0, 1 }  //RR
                            };
                        case 6:
                            return new float[,]
                            {
                                { 1, 0 },       //FL
                                { 0, 1 },       //FR
                                { 0.5f, 0.5f }, //C
                                { 0.5f, 0.5f }, //LFE
                                { 1, 0 },       //RL
                                { 0, 1 }        //RR
                            };
                        case 8:
                            return new float[,]
                            {
                                { 1, 0 },       //FL
                                { 0, 1 },       //FR
                                { 0.5f, 0.5f }, //C
                                { 0.5f, 0.5f }, //LFE
                                { 1, 0 },       //RL
                                { 0, 1 },       //RR
                                { 1, 0 },       //LC
                                { 0, 1 }        //RC
                            };
                    }
                    break;
            }
            return null;
        }
    }
}
