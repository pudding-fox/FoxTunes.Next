using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Dts;
using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public class BassDtsStreamProvider : BassStreamProvider, IConfigurableComponent
    {
        public const string DTS = "dts";

        public const string WAV = "wav";

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(BassDtsStreamProvider).Assembly.Location);
            }
        }

        public BassDtsStreamProvider()
        {
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", BassLoader.DIRECTORY_NAME_ADDON));
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", "bass_dts.dll"));
        }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement ProbeWavFiles { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.ProbeWavFiles = this.Configuration.GetElement<BooleanConfigurationElement>(
                BassDtsStreamProviderConfiguration.SECTION,
                BassDtsStreamProviderConfiguration.PROBE_WAV_FILES
            );
            base.InitializeComponent(core);
        }

        public override bool CanCreateStream(PlaylistItem playlistItem)
        {
            var extension = playlistItem.FileName.GetExtension();
            if (string.Equals(extension, DTS, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
            else if (string.Equals(extension, WAV, StringComparison.OrdinalIgnoreCase))
            {
                if (this.ProbeWavFiles.Value)
                {
                    Logger.Write(this, LogLevel.Debug, "Probing file \"{0}\" for DTS stream..", playlistItem.FileName);
                    var channelHandle = BassDts.CreateStream(playlistItem.FileName);
                    if (channelHandle != 0)
                    {
                        Logger.Write(this, LogLevel.Debug, "Found DTS stream in file \"{0}\".", playlistItem.FileName);
                        Bass.StreamFree(channelHandle);
                        return true;
                    }
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Probing for DTS streams is disabled.");
                }
            }
            return false;
        }

        public override IBassStream CreateBasicStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, BassFlags flags)
        {
            var fileName = this.GetFileName(playlistItem, advice);
            var channelHandle = BassDts.CreateStream(fileName, 0, 0, flags);
            if (channelHandle == 0)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create DTS stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
            }
            return this.CreateBasicStream(channelHandle, advice, flags);
        }

        public override IBassStream CreateInteractiveStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, bool immidiate, BassFlags flags)
        {
            var fileName = this.GetFileName(playlistItem, advice);
            var channelHandle = BassDts.CreateStream(fileName, 0, 0, flags);
            if (channelHandle == 0)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create DTS stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
            }
            return this.CreateInteractiveStream(channelHandle, advice, flags);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BassDtsStreamProviderConfiguration.GetConfigurationSections();
        }
    }
}
