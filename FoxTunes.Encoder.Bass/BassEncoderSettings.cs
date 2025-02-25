using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Linq;

namespace FoxTunes
{
    public abstract class BassEncoderSettings : BaseComponent, IBassEncoderSettings
    {
        public const int DEPTH_AUTO = 0;

        public const int DEPTH_16 = 16;

        public const int DEPTH_24 = 24;

        public const int DEPTH_32 = 32;

        public abstract string Name { get; }

        public abstract string Extension { get; }

        public abstract IBassEncoderFormat Format { get; }

        public virtual BassEncoderSettingsFlags Flags
        {
            get
            {
                return BassEncoderSettingsFlags.None;
            }
        }

        public virtual int GetDepth(EncoderItem encoderItem, IBassStream stream)
        {
            if (this.Format.AutoDepth)
            {
                switch (encoderItem.BitsPerSample)
                {
                    case DEPTH_16:
                    case DEPTH_24:
                    case DEPTH_32:
                        Logger.Write(this, LogLevel.Debug, "Using meta data suggested bit depth for file \"{0}\": {1} bit", encoderItem.InputFileName, encoderItem.BitsPerSample);
                        return encoderItem.BitsPerSample;
                }
                var channelInfo = default(ChannelInfo);
                if (!Bass.ChannelGetInfo(stream.ChannelHandle, out channelInfo))
                {
                    throw new NotImplementedException();
                }
                if (channelInfo.Flags.HasFlag(BassFlags.Float))
                {
                    Logger.Write(this, LogLevel.Debug, "Using decoder bit depth for file \"{0}\": 32 bit", encoderItem.InputFileName);
                    return DEPTH_32;
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Using decoder bit depth for file \"{0}\": 16 bit", encoderItem.InputFileName);
                    return DEPTH_16;
                }
            }
            Logger.Write(this, LogLevel.Debug, "Using user defined bit depth for file \"{0}\": {1} bit", encoderItem.InputFileName, this.Format.Depth);
            return this.Format.Depth;
        }

        public virtual int GetRate(EncoderItem encoderItem, IBassStream stream)
        {
            var sampleRates = this.Format.SampleRates;
            if (sampleRates == null || !sampleRates.Any())
            {
                return OutputRate.PCM_44100;
            }
            var channelInfo = default(ChannelInfo);
            if (!Bass.ChannelGetInfo(stream.ChannelHandle, out channelInfo))
            {
                throw new NotImplementedException();
            }
            foreach (var sampleRate in sampleRates)
            {
                if (sampleRate >= channelInfo.Frequency)
                {
                    return sampleRate;
                }
            }
            return sampleRates.LastOrDefault();
        }

        public virtual long GetLength(EncoderItem encoderItem, IBassStream stream)
        {
            var source = default(int);
            var channelInfo = default(ChannelInfo);
            var inputLength = stream.Length;
            if (!Bass.ChannelGetInfo(stream.ChannelHandle, out channelInfo))
            {
                throw new NotImplementedException();
            }
            if (channelInfo.Flags.HasFlag(BassFlags.Float))
            {
                source = DEPTH_32;
            }
            else
            {
                source = DEPTH_16;
            }
            var outputLength = (long)(inputLength / (source / (float)this.GetDepth(encoderItem, stream)));
            if (inputLength != outputLength)
            {
                Logger.Write(this, LogLevel.Debug, "Conversion requires change of data length: {0} bytes => {1} bytes.", inputLength, outputLength);
            }
            return outputLength;
        }

        public virtual int GetChannels(EncoderItem encoderItem, IBassStream stream)
        {
            var channelInfo = default(ChannelInfo);
            if (!Bass.ChannelGetInfo(stream.ChannelHandle, out channelInfo))
            {
                throw new NotImplementedException();
            }
            return channelInfo.Channels;
        }
    }
}
