using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Ffmpeg;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public class BassFfmpegStreamProvider : BassStreamProvider, IConfigurableComponent
    {
        const string DELIMITER = ",";

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(BassFfmpegStreamProvider).Assembly.Location);
            }
        }

        public BassFfmpegStreamProvider()
        {
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", BassLoader.DIRECTORY_NAME_ADDON));
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", "bass_ffmpeg.dll"));
        }

        public IConfiguration Configuration { get; private set; }

        public string[] Extensions { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<TextConfigurationElement>(
                BassDtsStreamProviderConfiguration.SECTION,
                BassDtsStreamProviderConfiguration.EXTENSIONS
            ).ConnectValue(value =>
            {
                this.Extensions = this.Parse(value);
            });
            base.InitializeComponent(core);
        }

        protected virtual string[] Parse(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return new string[] { };
            }
            return value
                .Split(new[] { DELIMITER }, StringSplitOptions.RemoveEmptyEntries)
                .Select(element => element.Trim())
                .ToArray();
        }


        public override bool CanCreateStream(PlaylistItem playlistItem)
        {
            if (this.Extensions == null || !this.Extensions.Any())
            {
                return false;
            }
            return FileSystemHelper.HasExtension(playlistItem.FileName, this.Extensions);
        }

        public override IBassStream CreateBasicStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, BassFlags flags)
        {
            var fileName = this.GetFileName(playlistItem, advice);
            var channelHandle = BassFfmpeg.CreateStream(fileName, 0, 0, flags);
            if (channelHandle == 0)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create FFMPEG stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
            }
            return this.CreateBasicStream(channelHandle, advice, flags);
        }

        public override IBassStream CreateInteractiveStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, bool immidiate, BassFlags flags)
        {
            var fileName = this.GetFileName(playlistItem, advice);
            var channelHandle = BassFfmpeg.CreateStream(fileName, 0, 0, flags);
            if (channelHandle == 0)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create FFMPEG stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
            }
            return this.CreateInteractiveStream(channelHandle, advice, flags);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BassDtsStreamProviderConfiguration.GetConfigurationSections();
        }
    }
}
