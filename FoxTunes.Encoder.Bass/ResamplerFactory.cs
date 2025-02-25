using FoxTunes.Interfaces;
using ManagedBass;
using System;

namespace FoxTunes
{
    public static class ResamplerFactory
    {
        public static Resampler Create(EncoderItem encoderItem, IBassStream stream, IBassEncoderSettings settings)
        {
            var inputFormat = GetInputFormat(encoderItem, stream, settings);
            var outputFormat = GetOutputFormat(encoderItem, stream, settings);
            return new Resampler(inputFormat, outputFormat);
        }

        public static Resampler.ResamplerFormat GetInputFormat(EncoderItem encoderItem, IBassStream stream, IBassEncoderSettings settings)
        {
            var channelInfo = default(ChannelInfo);
            if (!Bass.ChannelGetInfo(stream.ChannelHandle, out channelInfo))
            {
                throw new NotImplementedException();
            }

            var format = default(BassEncoderBinaryFormat);
            //I think all supported platforms are little endian.
            var endian = BassEncoderBinaryEndian.Little;
            var depth = default(int);
            if (channelInfo.Flags.HasFlag(BassFlags.Float))
            {
                format = BassEncoderBinaryFormat.FloatingPoint;
                depth = 32;
            }
            else
            {
                format = BassEncoderBinaryFormat.SignedInteger;
                depth = 16;
            }
            var rate = channelInfo.Frequency;
            var channels = channelInfo.Channels;
            return new Resampler.ResamplerFormat(format, endian, depth, rate, channels);
        }

        public static Resampler.ResamplerFormat GetOutputFormat(EncoderItem encoderItem, IBassStream stream, IBassEncoderSettings settings)
        {
            var channelInfo = default(ChannelInfo);
            if (!Bass.ChannelGetInfo(stream.ChannelHandle, out channelInfo))
            {
                throw new NotImplementedException();
            }
            var format = settings.Format.BinaryFormat;
            var endian = settings.Format.BinaryEndian;
            var depth = settings.GetDepth(encoderItem, stream);
            var rate = settings.GetRate(encoderItem, stream);
            var channels = settings.GetChannels(encoderItem, stream);
            return new Resampler.ResamplerFormat(format, endian, depth, rate, channels);
        }
    }
}
