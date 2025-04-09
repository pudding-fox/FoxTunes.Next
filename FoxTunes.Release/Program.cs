using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public class Program
    {
        const string DISTRIBUTION = "distribution";

        const string NIGHTLY = "nightly";

        const string DEFAULT = "main";

        const string MINIMAL = "minimal";

        const string PLUGINS = "plugins";

        const string NET40 = "net40";

        const string NET48 = "net48";

        const string LIB = "lib";

        const string L10N_FR = "fr";

        public static Package Launcher = GetLauncher();

        public static Package Core = GetCore();

        public static Package[] Plugins = GetPlugins();

        public static void Main(string[] args)
        {
            var version = string.Join(string.Empty, args);
            if (string.IsNullOrEmpty(version))
            {
                version = string.Format("{0}-{1}", DateTime.UtcNow.ToString("ddmmyyyy"), NIGHTLY);
            }
            Console.WriteLine("Version: {0}", version);
            CreateRelease(version, ReleaseFlags.FrameworkNET40 | ReleaseFlags.L10N_FR);
            CreateRelease(version, ReleaseFlags.FrameworkNET48 | ReleaseFlags.L10N_FR);
        }

        private static void CreateRelease(string version, ReleaseFlags flags)
        {
            var target = GetTarget(flags);
            var source = GetSource(target, flags);

            if (!Directory.Exists(source))
            {
                Console.WriteLine("Source was not build: {0}", source);
                return;
            }

            Console.WriteLine("Creating release: {0}", target);
            if (Directory.Exists(target))
            {
                Console.WriteLine("Removing previous release..");
                Directory.Delete(target, true);
            }
            Directory.CreateDirectory(target);

            Console.WriteLine("Creating base files..");

            AddPackage(Path.Combine(target, DEFAULT), Launcher, flags);
            AddPackage(Path.Combine(target, MINIMAL), Launcher, flags);

            AddPackage(Path.Combine(target, DEFAULT, LIB), Core, flags);
            AddPackage(Path.Combine(target, MINIMAL, LIB), Core, flags);

            Console.WriteLine("Installing plugins..");

            foreach (var plugin in Plugins)
            {
                Console.WriteLine("Installing plugin: {0}", plugin.Name);

                if (plugin.Flags.HasFlag(PackageFlags.Default))
                {
                    AddPackage(Path.Combine(target, DEFAULT, LIB), plugin, flags);
                }

                if (plugin.Flags.HasFlag(PackageFlags.Minimal))
                {
                    AddPackage(Path.Combine(target, MINIMAL, LIB), plugin, flags);
                }

                AddPackage(Path.Combine(target, PLUGINS), plugin, flags, true);
            }
        }

        private static void AddPackage(string target, Package package, ReleaseFlags flags, bool force = false)
        {
            if (!force)
            {
                //Filter by framework.
                if (package.Flags.HasFlag(PackageFlags.FrameworkNET48))
                {
                    if (flags.HasFlag(ReleaseFlags.FrameworkNET48) && !package.Flags.HasFlag(PackageFlags.FrameworkNET48))
                    {
                        return;
                    }
                }
            }

            foreach (var element in package.Elements)
            {
                AddPackageElement(target, package, element, flags);
            }
        }

        private static void AddPackageElement(string target, Package package, PackageElement element, ReleaseFlags flags)
        {
            //Filter by framework.
            if (element.Flags.HasFlag(PackageElementFlags.FrameworkNET48))
            {
                if (flags.HasFlag(ReleaseFlags.FrameworkNET48) && !element.Flags.HasFlag(PackageElementFlags.FrameworkNET48))
                {
                    return;
                }
            }

            var source = GetSource(target, package, element, flags);

            if (!File.Exists(source))
            {
                Console.WriteLine("File not found: {0}", source);
                return;
            }

            var destination = GetDestination(target, package, element, flags);

            CopyFile(source, destination, flags);
        }

        private static void CopyFile(string source, string destination, ReleaseFlags flags)
        {
            var directoryName = Path.GetDirectoryName(destination);
            if (!Directory.Exists(directoryName))
            {
                Console.WriteLine("Creating directory: {0}", directoryName);
                Directory.CreateDirectory(directoryName);
            }

            Console.WriteLine("Creating file: {0}", destination);
            File.Copy(source, destination);

            CopyResouces(source, destination, flags);
        }

        private static void CopyResouces(string source, string destination, ReleaseFlags flags)
        {
            if (flags.HasFlag(ReleaseFlags.L10N_FR))
            {
                CopyResouces(source, destination, L10N_FR, flags);
            }
        }

        private static void CopyResouces(string source, string destination, string culture, ReleaseFlags flags)
        {
            var sourceDirectoryName = Path.Combine(Path.GetDirectoryName(source), culture);
            if (!Directory.Exists(sourceDirectoryName))
            {
                return;
            }
            var fileNames = Directory.GetFiles(sourceDirectoryName, string.Format("{0}.resources.dll", Path.GetFileNameWithoutExtension(source)));
            if (fileNames.Length == 0)
            {
                return;
            }
            var destinationDirectoryName = Path.Combine(Path.GetDirectoryName(destination), culture);
            if (!Directory.Exists(destinationDirectoryName))
            {
                Console.WriteLine("Creating directory: {0}", destinationDirectoryName);
                Directory.CreateDirectory(destinationDirectoryName);
            }
            foreach (var fileName in fileNames)
            {
                Console.WriteLine("Creating satellite: {0}", destination);
                File.Copy(fileName, Path.Combine(destinationDirectoryName, Path.GetFileName(fileName)));
            }
        }

        private static string GetSource(string target, ReleaseFlags flags)
        {
            var parts = new List<string>()
            {
                "..",
                DISTRIBUTION
            };
            if (flags.HasFlag(ReleaseFlags.FrameworkNET40))
            {
                parts.Add(NET40);
            }
            if (flags.HasFlag(ReleaseFlags.FrameworkNET48))
            {
                parts.Add(NET48);
            }
            return Path.Combine(parts.ToArray());
        }

        private static string GetSource(string target, Package package, PackageElement element, ReleaseFlags flags)
        {
            return Path.Combine(GetSource(target, flags), element.FileName);
        }

        private static string GetDestination(string target, Package package, PackageElement element, ReleaseFlags flags)
        {
            var parts = new List<string>()
            {
                target
            };
            if (!string.IsNullOrEmpty(package.Name))
            {
                parts.Add(package.Name);
            }
            if (!string.IsNullOrEmpty(element.FolderName))
            {
                parts.Add(element.FolderName);
                parts.Add(Path.GetFileName(element.FileName));
            }
            else
            {
                parts.Add(element.FileName);
            }
            return Path.Combine(parts.ToArray());
        }


        private static string GetTarget(ReleaseFlags flags)
        {
            var parts = new List<string>();
            if (flags.HasFlag(ReleaseFlags.FrameworkNET40))
            {
                parts.Add(NET40);
            }
            if (flags.HasFlag(ReleaseFlags.FrameworkNET48))
            {
                parts.Add(NET48);
            }
            return Path.Combine(parts.ToArray());
        }

        private static Package GetLauncher()
        {
            return new Package(new PackageElement[]
            {
                "FoxTunes.Launcher.exe",
                "FoxTunes.Launcher.exe.config",
                "FoxTunes.Launcher.x86.exe",
                "FoxTunes.Launcher.x86.exe.config"
            });
        }

        private static Package GetCore()
        {
            return new Package(new PackageElement[]
            {
                "FoxDb.Core.dll",
                "FoxDb.Linq.dll",
                "FoxDb.Sql.dll",
                "FoxTunes.Core.dll",
                "FoxTunes.DB.dll",
                "FoxTunes.MetaData.dll",
                "FoxTunes.Output.dll",
                "FoxTunes.Scripting.dll",
                "FoxTunes.Scripting.JS.dll",
                "FoxTunes.UI.dll",
                new PackageElement("Microsoft.Threading.Tasks.Extensions.Desktop.dll", PackageElementFlags.FrameworkNET40),
                new PackageElement("Microsoft.Threading.Tasks.Extensions.dll", PackageElementFlags.FrameworkNET40),
                new PackageElement("Microsoft.Threading.Tasks.dll", PackageElementFlags.FrameworkNET40),
                new PackageElement("Microsoft.Windows.Shell.dll", PackageElementFlags.FrameworkNET40),
                new PackageElement("System.IO.dll", PackageElementFlags.FrameworkNET40),
                new PackageElement("System.Runtime.dll",PackageElementFlags.FrameworkNET40),
                new PackageElement("System.Threading.Tasks.dll", PackageElementFlags.FrameworkNET40),
                "System.Windows.Interactivity.dll"
            });
        }

        private static Package[] GetPlugins()
        {
            return new Package[]
            {
                new Package(
                    "archive",
                    new PackageElement[]
                    {
                        "FoxTunes.Output.Bass.Archive.dll",
                        "ManagedBass.ZipStream.dll",
                        new PackageElement("x86/bass_zipstream.dll", "x86/addon"),
                        new PackageElement("x64/bass_zipstream.dll", "x64/addon")
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "asio",
                    new PackageElement[]
                    {
                        "x86/bass_asio_handler.dll",
                        "x86/bassasio.dll",
                        "x64/bass_asio_handler.dll",
                        "x64/bassasio.dll",
                        "FoxTunes.Output.Bass.Asio.dll",
                        "ManagedBass.Asio.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "bass",
                    new PackageElement[]
                    {
                        "x86/addon/bass_aac.dll",
                        "x86/addon/bass_ac3.dll",
                        "x86/addon/bass_ape.dll",
                        "x86/addon/bassalac.dll",
                        "x86/addon/bassflac.dll",
                        "x86/addon/bassmidi.dll",
                        "x86/addon/bassopus.dll",
                        "x86/addon/basswma.dll",
                        "x86/addon/basswv.dll",
                        "x86/bass.dll",
                        "x86/bass_fx.dll",
                        "x86/bassmix.dll",
                        "x64/addon/bass_aac.dll",
                        "x64/addon/bass_ac3.dll",
                        "x64/addon/bass_ape.dll",
                        "x64/addon/bassalac.dll",
                        "x64/addon/bassflac.dll",
                        "x64/addon/bassmidi.dll",
                        "x64/addon/bassopus.dll",
                        "x64/addon/basswma.dll",
                        "x64/addon/basswv.dll",
                        "x64/bass.dll",
                        "x64/bass_fx.dll",
                        "x64/bassmix.dll",
                        "FoxTunes.Output.Bass.dll",
                        "ManagedBass.dll",
                        "ManagedBass.Fx.dll",
                        "ManagedBass.Mix.dll"
                    },
                    PackageFlags.Default | PackageFlags.Minimal
                ),
                new Package(
                    "cd",
                    new PackageElement[]
                    {
                        "x86/addon/basscd.dll",
                        "x64/addon/basscd.dll",
                        "FoxTunes.Output.Bass.Cd.dll",
                        "ManagedBass.Cd.dll",
                        new PackageElement("x86/bass_gapless_cd.dll", "x86/addon"),
                        new PackageElement("x64/bass_gapless_cd.dll", "x64/addon")
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "conf",
                    new PackageElement[]
                    {
                        "FoxTunes.Config.dll"
                    },
                    PackageFlags.Default | PackageFlags.Minimal
                ),
                new Package(
                    "crossfade",
                    new PackageElement[]
                    {
                        "FoxTunes.Output.Bass.Crossfade.dll",
                        "ManagedBass.Crossfade.dll",
                        new PackageElement("x86/bass_crossfade.dll", "x86/addon"),
                        new PackageElement("x64/bass_crossfade.dll", "x64/addon")
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "cue",
                    new PackageElement[]
                    {
                        "FoxTunes.Output.Bass.Cue.dll",
                        "ManagedBass.Substream.dll",
                        new PackageElement("x86/bass_substream.dll", "x86/addon"),
                        new PackageElement("x64/bass_substream.dll", "x64/addon")
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "dialogs",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.Dialogs.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "directsound",
                    new PackageElement[]
                    {
                        "FoxTunes.Output.Bass.DirectSound.dll"
                    },
                    PackageFlags.Default | PackageFlags.Minimal
                ),
                new Package(
                    "discogs",
                    new PackageElement[]
                    {
                        "FoxTunes.MetaData.Discogs.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "discord",
                    new PackageElement[]
                    {
                        "FoxTunes.Discord.dll",
                        "Discord.Sharp.dll",
                        new PackageElement("x86/discord.dll", "x86"),
                        new PackageElement("x86/discord-rpc.dll", "x86"),
                        new PackageElement("x64/discord.dll", "x64"),
                        new PackageElement("x64/discord-rpc.dll", "x64")
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "dsd",
                    new PackageElement[]
                    {
                        "x86/addon/bassdsd.dll",
                        "x64/addon/bassdsd.dll",
                        "FoxTunes.Output.Bass.Dsd.dll",
                        "ManagedBass.Dsd.dll",
                        "SacdSharp.dll",
                        new PackageElement("x86/bass_memory_dsd.dll", "x86/addon"),
                        new PackageElement("x64/bass_memory_dsd.dll", "x64/addon"),
                        "x86/sacd_extract.exe"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "dts",
                    new PackageElement[]
                    {
                        "FoxTunes.Output.Bass.Dts.dll",
                        "ManagedBass.Dts.dll",
                        new PackageElement("x86/bass_dts.dll", "x86/addon"),
                        new PackageElement("x64/bass_dts.dll", "x64/addon")
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "encoder",
                    new PackageElement[]
                    {
                        "encoders/flac.exe",
                        "encoders/flac_license.txt",
                        "encoders/lame.exe",
                        "encoders/lame_license.txt",
                        "encoders/nsutil.dll",
                        "encoders/oggenc2.exe",
                        "encoders/opusenc.exe",
                        "encoders/opusenc_license.txt",
                        "encoders/refalac.exe",
                        "encoders/wavpack.exe",
                        "encoders/wavpack_license.txt",
                        "sox/LICENSE.GPL.txt",
                        "sox/msvcr120d.dll",
                        "sox/sox.exe",
                        "FoxTunes.Encoder.Bass.exe",
                        "FoxTunes.Encoder.Bass.exe.config"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "eq",
                    new PackageElement[]
                    {
                        "presets/Bass.txt",
                        "presets/Flat.txt",
                        "presets/Pop.txt",
                        "presets/Rock.txt",
                        "FoxTunes.Output.Bass.ParametricEqualizer.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "gapless",
                    new PackageElement[]
                    {
                        "FoxTunes.Output.Bass.Gapless.dll",
                        "ManagedBass.Gapless.dll",
                        new PackageElement("x86/bass_gapless.dll", "x86/addon"),
                        new PackageElement("x64/bass_gapless.dll", "x64/addon")
                    },
                    PackageFlags.Default | PackageFlags.Minimal
                ),
                new Package(
                    "noesis",
                    new PackageElement[]
                    {
                        "FoxTunes.Scripting.JS.Noesis.dll",
                        "x86/Noesis.Javascript.dll",
                        "x64/Noesis.Javascript.dll",
                        "x86/msvcp100.dll",
                        "x86/msvcr100.dll",
                        "x64/msvcp100.dll",
                        "x64/msvcr100.dll"
                    },
                    PackageFlags.Default | PackageFlags.Minimal
                ),
                new Package(
                    "clearscript",
                    new PackageElement[]
                    {
                        "FoxTunes.Scripting.JS.ClearScript.dll",
                        "ClearScript.Core.dll",
                        "ClearScript.Windows.dll",
                        "ClearScript.Windows.Core.dll",
                        "ClearScript.V8.dll",
                        "ClearScript.V8.ICUData.dll",
                        new PackageElement("ClearScriptV8.win-x86.dll", "x86"),
                        new PackageElement("ClearScriptV8.win-x64.dll", "x64"),
                        "Newtonsoft.Json.dll",
                    },
                    PackageFlags.Default | PackageFlags.Minimal | PackageFlags.FrameworkNET48
                ),
                new Package(
                     "v8net",
                     new PackageElement[]
                     {
                        "FoxTunes.Scripting.JS.V8Net.dll",
                        "V8.Net.dll",
                        "x86/V8_Net_Proxy_x86.dll",
                        "x64/V8_Net_Proxy_x64.dll"
                     }
                 ),
                new Package(
                    "groupedplaylist",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.GroupedPlaylist.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "layout",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.Layout.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "librarybrowser",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.LibraryBrowser.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "logger",
                    new PackageElement[]
                    {
                        "FoxTunes.Logging.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "lyrics",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.Lyrics.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "memory",
                    new PackageElement[]
                    {
                        "FoxTunes.Output.Bass.Memory.dll",
                        "ManagedBass.Memory.dll",
                        new PackageElement("x86/bass_memory.dll", "x86/addon"),
                        new PackageElement("x64/bass_memory.dll", "x64/addon")
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "minidisc",
                    new PackageElement[]
                    {
                        "FoxTunes.Minidisc.Bass.dll",
                        "MD.Net.dll",
                        "x86/atracdenc.exe",
                        "x86/gcrypt.dll",
                        "x86/himdcli.exe",
                        "x86/libgcc_s_dw2-1.dll",
                        "x86/libgcc_s_sjlj-1.dll",
                        "x86/libgcrypt-20.dll",
                        "x86/libglib-2.0-0.dll",
                        "x86/libgpg-error-0.dll",
                        "x86/libjson-c-5.dll",
                        "x86/libusb-1.0.dll",
                        "x86/libwinpthread-1.dll",
                        "x86/netmdcli.exe",
                        "x86/zlib1.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "miniplayer",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.MiniPlayer.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "metadataeditor",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.MetaDataEditor.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "metadataviewer",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.MetaDataViewer.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "mod",
                    new PackageElement[]
                    {
                        "x86/addon/basszxtune.dll",
                        "x64/addon/basszxtune.dll",
                        "FoxTunes.Output.Bass.Mod.dll"
                    }
                ),
                new Package(
                    "moodbar",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.MoodBar.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "ratings",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.Ratings.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "replaygain",
                    new PackageElement[]
                    {
                        "FoxTunes.Output.Bass.ReplayGain.exe",
                        "FoxTunes.Output.Bass.ReplayGain.exe.config",
                        "ManagedBass.ReplayGain.dll",
                        new PackageElement("x86/bass_replay_gain.dll", "x86/addon"),
                        new PackageElement("x64/bass_replay_gain.dll", "x64/addon")
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "simplemetadata",
                    new PackageElement[]
                    {
                        "FoxTunes.MetaData.FileName.dll"
                    }
                ),
                new Package(
                    "snapping",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.Snapping.dll"
                    }
                ),
                new Package(
                    "resampler",
                    new PackageElement[]
                    {
                        "FoxTunes.Output.Bass.Resampler.dll",
                        "ManagedBass.Sox.dll",
                        new PackageElement("x86/bass_sox.dll", "x86/addon"),
                        new PackageElement("x64/bass_sox.dll", "x64/addon")
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "visualizations",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.Visualizations.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "sqlite",
                    new PackageElement[]
                    {
                        "FoxDb.SQLite.dll",
                        "FoxTunes.DB.SQLite.dll",
                        "x86/SQLite.Interop.dll",
                        "x64/SQLite.Interop.dll",
                        "System.Data.SQLite.dll"
                    },
                    PackageFlags.Default | PackageFlags.Minimal
                ),
                new Package(
                    "sqlserver",
                    new PackageElement[]
                    {
                        "FoxDb.SqlServer.2012.dll",
                        "FoxDb.SqlServer.dll",
                        "FoxTunes.DB.SqlServer.dll"
                    }
                ),
                new Package(
                    "statistics",
                    new PackageElement[]
                    {
                        "FoxTunes.Statistics.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "taglibmetadata",
                    new PackageElement[]
                    {
                        "FoxTunes.MetaData.TagLib.dll",
                        "taglib-sharp.dll"
                    },
                    PackageFlags.Default | PackageFlags.Minimal
                ),
                new Package(
                    "tempo",
                    new PackageElement[]
                    {
                        "FoxTunes.Output.Bass.Tempo.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "tools",
                    new PackageElement[]
                    {
                        "FoxTunes.Tools.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "wasapi",
                    new PackageElement[]
                    {
                        "x86/bass_wasapi_handler.dll",
                        "x86/basswasapi.dll",
                        "x64/bass_wasapi_handler.dll",
                        "x64/basswasapi.dll",
                        "FoxTunes.Output.Bass.Wasapi.dll",
                        "ManagedBass.Wasapi.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "wavebar",
                    new PackageElement[]
                    {
                        "FoxTunes.UI.Windows.WaveBar.dll"
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "windows",
                    new PackageElement[]
                    {
                        "FoxTunes.Core.Windows.dll",
                        new PackageElement("FoxTunes.Core.Windows.UWP.dll", PackageElementFlags.FrameworkNET48),
                        new PackageElement("System.Runtime.WindowsRuntime.dll", PackageElementFlags.FrameworkNET48)
                    },
                    PackageFlags.Default
                ),
                new Package(
                    "wpf",
                    new PackageElement[]
                    {
                        "x86/bitmap_utilities.dll",
                        "x64/bitmap_utilities.dll",
                        "FoxTunes.UI.Windows.dll",
                        "FoxTunes.UI.Windows.Themes.dll"
                    },
                    PackageFlags.Default | PackageFlags.Minimal
                )
            };
        }

        [Flags]
        public enum ReleaseFlags : byte
        {
            None = 0,
            FrameworkNET40 = 1,
            FrameworkNET48 = 4,
            L10N_FR = 32
        }

        public class Package
        {
            public Package(PackageElement[] elements, PackageFlags flags = PackageFlags.None) : this(null, elements, flags)
            {

            }

            public Package(string name, PackageElement[] elements, PackageFlags flags = PackageFlags.None)
            {
                this.Name = name;
                this.Elements = elements;
                this.Flags = flags;
            }

            public string Name { get; private set; }

            public PackageElement[] Elements { get; private set; }

            public PackageFlags Flags { get; private set; }
        }

        [Flags]
        public enum PackageFlags : byte
        {
            None = 0,
            Default = 1,
            Minimal = 2,
            FrameworkNET48 = 16
        }

        public class PackageElement
        {
            public PackageElement(string fileName, PackageElementFlags flags = PackageElementFlags.None)
            {
                this.FileName = fileName;
                this.Flags = flags;
            }

            public PackageElement(string fileName, string folderName, PackageElementFlags flags = PackageElementFlags.None) : this(fileName, flags)
            {
                this.FolderName = folderName;
            }

            public string FileName { get; private set; }

            public string FolderName { get; private set; }

            public PackageElementFlags Flags { get; private set; }

            public static implicit operator PackageElement(string fileName)
            {
                return new PackageElement(fileName, PackageElementFlags.None);
            }
        }

        [Flags]
        public enum PackageElementFlags : byte
        {
            None = 0,
            FrameworkNET40 = 1,
            FrameworkNET48 = 4
        }
    }
}
