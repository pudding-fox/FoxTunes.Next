using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.IO;

namespace FoxTunes
{
    public class Wav_16_44100_Settings : BassEncoderHandler, IStandardComponent
    {
        public const string NAME = "043426B6-7251-45EB-BD74-E46D6ED97A83";

        public override string Name
        {
            get
            {
                return NAME;
            }
        }

        public override string Extension
        {
            get
            {
                return "wav";
            }
        }

        public override IBassEncoderFormat Format
        {
            get
            {
                return new BassEncoderFormat(BassEncoderBinaryFormat.SignedInteger, BassEncoderBinaryEndian.Little, DEPTH_16, OutputRate.PCM_44100);
            }
        }

        public override BassEncoderSettingsFlags Flags
        {
            get
            {
                return BassEncoderSettingsFlags.Internal;
            }
        }

        public override IBassEncoderWriter GetWriter(EncoderItem encoderItem, IBassStream stream)
        {
            var channelInfo = default(ChannelInfo);
            if (!Bass.ChannelGetInfo(stream.ChannelHandle, out channelInfo))
            {
                throw new NotImplementedException();
            }
            var length = Bass.ChannelGetLength(stream.ChannelHandle, PositionFlags.Bytes);
            return new Writer(File.Create(encoderItem.OutputFileName), channelInfo, length);
        }

        public class Writer : BassEncoderWriter
        {
            public Writer(Stream stream, ChannelInfo channelInfo, long length) : base(stream)
            {
                this.WriteHeader(channelInfo, length);
            }

            protected virtual void WriteHeader(ChannelInfo channelInfo, long length)
            {
                var info = default(WavHeader.WavInfo);
                info.Format = WavHeader.WAV_FORMAT_PCM;
                info.ChannelCount = channelInfo.Channels;
                info.SampleRate = OutputRate.PCM_44100;
                info.ByteRate = (OutputRate.PCM_44100 * DEPTH_16 * channelInfo.Channels) / 8;
                info.BlockAlign = (DEPTH_16 * channelInfo.Channels) / 8;
                info.BitsPerSample = DEPTH_16;
                if (channelInfo.Flags.HasFlag(BassFlags.Float))
                {
                    info.DataSize = Convert.ToInt32(length / 2);
                }
                else
                {
                    info.DataSize = Convert.ToInt32(length);
                }
                WavHeader.Write(this.Stream, info);
            }
        }
    }
}
