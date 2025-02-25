using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public class OpusEncoderSettings : BassEncoderTool, IStandardComponent, IConfigurableComponent
    {
        public override string Executable
        {
            get
            {
                var directory = Path.GetDirectoryName(
                    typeof(OpusEncoderSettings).Assembly.Location
                );
                return Path.Combine(directory, "Encoders\\opusenc.exe");
            }
        }

        public override string Name
        {
            get
            {
                return "Opus";
            }
        }

        public override string Extension
        {
            get
            {
                return "opus";
            }
        }

        public override IBassEncoderFormat Format
        {
            get
            {
                //TODO: Assuming signed integer but command line doesn't specify.
                return new BassEncoderFormat(BassEncoderBinaryFormat.SignedInteger, BassEncoderBinaryEndian.Little, DEPTH_16);
            }
        }

        public int Bitrate { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            core.Components.Configuration.GetElement<IntegerConfigurationElement>(
                OpusEncoderSettingsConfiguration.SECTION,
                OpusEncoderSettingsConfiguration.BITRATE_ELEMENT
            ).ConnectValue(value => this.Bitrate = value);
            base.InitializeComponent(core);
        }

        public override string GetArguments(EncoderItem encoderItem, IBassStream stream)
        {
            var channelInfo = default(ChannelInfo);
            if (!Bass.ChannelGetInfo(stream.ChannelHandle, out channelInfo))
            {
                throw new NotImplementedException();
            }
            return string.Format(
                "--quiet --raw --raw-rate {0} --raw-chan {1} --raw-bits {2} --raw-endianness 0 - --hard-cbr --bitrate {3} \"{4}\"",
                this.GetRate(encoderItem, stream),
                channelInfo.Channels,
                this.GetDepth(encoderItem, stream),
                this.Bitrate,
                encoderItem.OutputFileName
            );
        }

        public override int GetDepth(EncoderItem encoderItem, IBassStream stream)
        {
            return DEPTH_16;
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return OpusEncoderSettingsConfiguration.GetConfigurationSections(this);
        }
    }
}
