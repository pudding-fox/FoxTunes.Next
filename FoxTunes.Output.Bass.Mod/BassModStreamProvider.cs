using FoxTunes;
using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public class BassModStreamProvider : BassStreamProvider
    {
        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(BassModStreamProvider).Assembly.Location);
            }
        }

        public static readonly string[] EXTENSIONS = new[]
        {
            "$b",
            "$m",
            "2sf",
            "ahx",
            "as0",
            "asc",
            "ay",
            "ayc",
            "bin",
            "cc3",
            "chi",
            "cop",
            "d",
            "dmm",
            "dsf",
            "dsq",
            "dst",
            "esv",
            "fdi",
            "ftc",
            "gam",
            "gamplus",
            "gbs",
            "gsf",
            "gtr",
            "gym",
            "hes",
            "hrm",
            "hrp",
            "hvl",
            "it",
            "kss",
            "lzs",
            "m",
            "mo3",
            "mod",
            "mod",
            "mptm",
            "msp",
            "mtc",
            "mtm",
            "nsf",
            "nsfe",
            "p",
            "pcd",
            "psc",
            "psf",
            "psf2",
            "psg",
            "psm",
            "pt1",
            "pt2",
            "pt3",
            "rmt",
            "rsn",
            "s",
            "s3m",
            "sap",
            "scl",
            "sid",
            "spc",
            "sqd",
            "sqt",
            "ssf",
            "st1",
            "st3",
            "stc",
            "stp",
            "str",
            "szx",
            "td0",
            "tf0",
            "tfc",
            "tfd",
            "tfe",
            "tlz",
            "tlzp",
            "trd",
            "trs",
            "ts",
            "umx",
            "usf",
            "vgm",
            "vgz",
            "vtx",
            "xm",
            "ym"
        };

        public BassModStreamProvider()
        {
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", BassLoader.DIRECTORY_NAME_ADDON));
            //basszxtune.dll does not register all possible extensions.
            BassLoader.AddExtensions(EXTENSIONS);
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
            var channelHandle = Bass.MusicLoad(fileName, 0, 0, flags | BassFlags.Prescan);
            if (channelHandle == 0)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create MOD stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
            }
            return this.CreateBasicStream(channelHandle, advice, flags);
        }

        public override IBassStream CreateInteractiveStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, bool immidiate, BassFlags flags)
        {
            var fileName = this.GetFileName(playlistItem, advice);
            var channelHandle = Bass.MusicLoad(fileName, 0, 0, flags | BassFlags.Prescan);
            if (channelHandle == 0)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create MOD stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
            }
            return this.CreateInteractiveStream(channelHandle, advice, flags);
        }

        public override void FreeStream(int channelHandle)
        {
            Logger.Write(this, LogLevel.Debug, "Freeing stream: {0}", channelHandle);
            Bass.MusicFree(channelHandle); //Not checking result code as it contains an error if the application is shutting down.
        }
    }
}
