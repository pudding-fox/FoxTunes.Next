using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public class LameEncoderSettings : BassEncoderTool, IStandardComponent, IConfigurableComponent
    {
        public override string Executable
        {
            get
            {
                var directory = Path.GetDirectoryName(
                    typeof(LameEncoderSettings).Assembly.Location
                );
                return Path.Combine(directory, "Encoders\\lame.exe");
            }
        }

        public override string Name
        {
            get
            {
                return "MP3";
            }
        }

        public override string Extension
        {
            get
            {
                return "mp3";
            }
        }

        public override IBassEncoderFormat Format
        {
            get
            {
                return new BassEncoderFormat(BassEncoderBinaryFormat.SignedInteger, BassEncoderBinaryEndian.Little, DEPTH_16);
            }
        }

        public int Bitrate { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            core.Components.Configuration.GetElement<SelectionConfigurationElement>(
                LameEncoderSettingsConfiguration.SECTION,
                LameEncoderSettingsConfiguration.BITRATE_ELEMENT
            ).ConnectValue(option => this.Bitrate = LameEncoderSettingsConfiguration.GetBitrate(option));
            base.InitializeComponent(core);
        }

        public override string GetArguments(EncoderItem encoderItem, IBassStream stream)
        {
            return string.Format(
                "--silent -r --little-endian --signed -s {0} -m {1} --bitwidth {2} - --cbr -b {3} \"{4}\"",
                this.GetRate(encoderItem, stream),
                this.GetStereoMode(encoderItem, stream),
                this.GetDepth(encoderItem, stream),
                this.Bitrate,
                encoderItem.OutputFileName
            );
        }

        protected virtual string GetStereoMode(EncoderItem encoderItem, IBassStream stream)
        {
            switch (this.GetChannels(encoderItem, stream))
            {
                case 1:
                    return "m";
                default:
                case 2:
                    return "s";
            }
        }

        public override int GetDepth(EncoderItem encoderItem, IBassStream stream)
        {
            return DEPTH_16;
        }

        public override int GetChannels(EncoderItem encoderItem, IBassStream stream)
        {
            var channels = base.GetChannels(encoderItem, stream);
            return Math.Min(channels, 2);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return LameEncoderSettingsConfiguration.GetConfigurationSections(this);
        }
    }
}
