using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public class WavPackEncoderSettings : BassEncoderTool, IStandardComponent, IConfigurableComponent
    {
        public override string Executable
        {
            get
            {
                var directory = Path.GetDirectoryName(
                    typeof(WavPackEncoderSettings).Assembly.Location
                );
                return Path.Combine(directory, "Encoders\\wavpack.exe");
            }
        }

        public override string Name
        {
            get
            {
                return "WavPack";
            }
        }

        public override string Extension
        {
            get
            {
                return "wv";
            }
        }

        public override IBassEncoderFormat Format
        {
            get
            {
                return new BassEncoderFormat(BassEncoderBinaryFormat.SignedInteger, BassEncoderBinaryEndian.Little, this.Depth);
            }
        }

        public int Depth { get; private set; }

        public int Compression { get; private set; }

        public int Processing { get; private set; }

        public int Bitrate { get; private set; }

        public bool Hybrid { get; private set; }

        public bool Correction { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            core.Components.Configuration.GetElement<SelectionConfigurationElement>(
                WavPackSettingsConfiguration.SECTION,
                WavPackSettingsConfiguration.DEPTH_ELEMENT
            ).ConnectValue(option => this.Depth = WavPackSettingsConfiguration.GetDepth(option));
            core.Components.Configuration.GetElement<IntegerConfigurationElement>(
                WavPackSettingsConfiguration.SECTION,
                WavPackSettingsConfiguration.COMPRESSION_ELEMENT
            ).ConnectValue(value => this.Compression = value);
            core.Components.Configuration.GetElement<IntegerConfigurationElement>(
                WavPackSettingsConfiguration.SECTION,
                WavPackSettingsConfiguration.PROCESSING_ELEMENT
            ).ConnectValue(value => this.Processing = value);
            core.Components.Configuration.GetElement<IntegerConfigurationElement>(
                WavPackSettingsConfiguration.SECTION,
                WavPackSettingsConfiguration.BITRATE_ELEMENT
            ).ConnectValue(value => this.Bitrate = value);
            core.Components.Configuration.GetElement<BooleanConfigurationElement>(
                WavPackSettingsConfiguration.SECTION,
                WavPackSettingsConfiguration.HYBRID_ELEMENT
            ).ConnectValue(value => this.Hybrid = value);
            core.Components.Configuration.GetElement<BooleanConfigurationElement>(
                WavPackSettingsConfiguration.SECTION,
                WavPackSettingsConfiguration.CORRECTION_ELEMENT
            ).ConnectValue(value => this.Correction = value);
            base.InitializeComponent(core);
        }

        public override string GetArguments(EncoderItem encoderItem, IBassStream stream)
        {
            return string.Format(
                "-i -q -y --raw-pcm={0} {1} -x{2} {3} - \"{4}\"",
                this.GetFormat(encoderItem, stream),
                this.GetCompression(),
                this.Processing,
                this.GetHybrid(),
                encoderItem.OutputFileName
            );
        }

        protected virtual string GetFormat(EncoderItem encoderItem, IBassStream stream)
        {
            var channelInfo = default(ChannelInfo);
            if (!Bass.ChannelGetInfo(stream.ChannelHandle, out channelInfo))
            {
                throw new NotImplementedException();
            }
            return string.Format(
                "{0},{1}s,{2},le",
                this.GetRate(encoderItem, stream),
                this.GetDepth(encoderItem, stream),
                channelInfo.Channels
            );
        }

        protected virtual string GetCompression()
        {
            switch (this.Compression)
            {
                case 0:
                    return "-f";
                default:
                case 1:
                    return string.Empty;
                case 2:
                    return "-h";
                case 3:
                    return "-hh";
            }
        }

        protected virtual string GetHybrid()
        {
            if (!this.Hybrid)
            {
                return string.Empty;
            }
            return string.Format(
                "-b{0} {1}",
                this.Bitrate,
                this.Correction ? "-c" : string.Empty
            );
        }

        public override int GetDepth(EncoderItem encoderItem, IBassStream stream)
        {
            var depth = base.GetDepth(encoderItem, stream);
            if (depth < DEPTH_16)
            {
                Logger.Write(this.GetType(), LogLevel.Warn, "Requsted bit depth {0} is invalid, using minimum: 16 bit", depth);
                return DEPTH_16;
            }
            if (depth > DEPTH_32)
            {
                Logger.Write(this.GetType(), LogLevel.Warn, "Requsted bit depth {0} is invalid, using maximum: 32 bit", depth);
                return DEPTH_32;
            }
            return depth;
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return WavPackSettingsConfiguration.GetConfigurationSections(this);
        }
    }
}
