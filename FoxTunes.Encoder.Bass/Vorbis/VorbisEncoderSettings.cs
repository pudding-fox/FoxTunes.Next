using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public class VorbisEncoderSettings : BassEncoderTool, IStandardComponent, IConfigurableComponent
    {
        public override string Executable
        {
            get
            {
                var directory = Path.GetDirectoryName(
                    typeof(VorbisEncoderSettings).Assembly.Location
                );
                return Path.Combine(directory, "Encoders\\oggenc2.exe");
            }
        }

        public override string Name
        {
            get
            {
                return "Ogg Vorbis";
            }
        }

        public override string Extension
        {
            get
            {
                return "ogg";
            }
        }

        public override IBassEncoderFormat Format
        {
            get
            {
                return new BassEncoderFormat(BassEncoderBinaryFormat.SignedInteger, BassEncoderBinaryEndian.Little, DEPTH_16);
            }
        }

        public int Quality { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            core.Components.Configuration.GetElement<IntegerConfigurationElement>(
                VorbisEncoderSettingsConfiguration.SECTION,
                VorbisEncoderSettingsConfiguration.QUALITY_ELEMENT
            ).ConnectValue(value => this.Quality = value);
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
                "--quiet --raw --raw-format=1 --raw-rate={0} --raw-chan={1} --raw-bits={2} --raw-endianness=0 - --quality={3} -o \"{4}\"",
                this.GetRate(encoderItem, stream),
                channelInfo.Channels,
                this.GetDepth(encoderItem, stream),
                this.Quality,
                encoderItem.OutputFileName
            );
        }

        public override int GetDepth(EncoderItem encoderItem, IBassStream stream)
        {
            return DEPTH_16;
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return VorbisEncoderSettingsConfiguration.GetConfigurationSections(this);
        }
    }
}
