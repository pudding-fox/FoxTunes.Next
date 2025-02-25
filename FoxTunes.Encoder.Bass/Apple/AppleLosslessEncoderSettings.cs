using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public class AppleLosslessEncoderSettings : BassEncoderTool, IStandardComponent, IConfigurableComponent
    {
        public override string Executable
        {
            get
            {
                var directory = Path.GetDirectoryName(
                    typeof(AppleLosslessEncoderSettings).Assembly.Location
                );
                return Path.Combine(directory, "Encoders\\refalac.exe");
            }
        }

        public override string Name
        {
            get
            {
                return "Apple Lossless";
            }
        }

        public override string Extension
        {
            get
            {
                return "m4a";
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

        public override void InitializeComponent(ICore core)
        {
            core.Components.Configuration.GetElement<SelectionConfigurationElement>(
                AppleLosslessEncoderSettingsConfiguration.SECTION,
                AppleLosslessEncoderSettingsConfiguration.DEPTH_ELEMENT
            ).ConnectValue(option => this.Depth = AppleLosslessEncoderSettingsConfiguration.GetDepth(option));
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
                "--alac --raw --raw-rate {0} --raw-channels {1} --raw-format {2} - --bits-per-sample {3} -o \"{4}\"",
                this.GetRate(encoderItem, stream),
                channelInfo.Channels,
                this.GetFormat(encoderItem, stream),
                this.GetDepth(encoderItem, stream),
                encoderItem.OutputFileName
            );
        }

        protected virtual string GetFormat(EncoderItem encoderItem, IBassStream stream)
        {
            return string.Format("S{0}L", this.GetDepth(encoderItem, stream));
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
            return AppleLosslessEncoderSettingsConfiguration.GetConfigurationSections(this);
        }
    }
}
