using FoxTunes;
using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Dts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public class BassDtsStreamProvider : BassStreamProvider
    {
        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(BassDtsStreamProvider).Assembly.Location);
            }
        }

        public static readonly string[] EXTENSIONS = new[]
        {
            "dts"
        };

        public BassDtsStreamProvider()
        {
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", BassLoader.DIRECTORY_NAME_ADDON));
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", "bass_dts.dll"));
        }

        public override bool CanCreateStream(PlaylistItem playlistItem)
        {
            if (!EXTENSIONS.Contains(playlistItem.FileName.GetExtension(), StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
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
    }
}
