using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public class BassCueStreamAdvisor : BassStreamAdvisor
    {
        public const string SCHEME = "cue";

        public BassCueStreamAdvisorBehaviour Behaviour { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Behaviour = ComponentRegistry.Instance.GetComponent<BassCueStreamAdvisorBehaviour>();
            base.InitializeComponent(core);
        }

        public override void Advise(IBassStreamProvider provider, PlaylistItem playlistItem, IList<IBassStreamAdvice> advice, BassStreamUsageType type)
        {
            if (this.Behaviour == null || !this.Behaviour.Enabled)
            {
                return;
            }

            var fileName = default(string);
            var offset = default(string);
            var length = default(string);
            try
            {
                if (!ParseUrl(playlistItem.FileName, out fileName, out offset, out length))
                {
                    return;
                }
                advice.Add(new BassCueStreamAdvice(fileName, offset, length));
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create stream advice for file \"{0}\": {1}", playlistItem.FileName, e.Message);
            }
        }

        public static string CreateUrl(string fileName, string offset)
        {
            return string.Format(
                "{0}://{1}?offset={2}",
                SCHEME,
                fileName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                offset
            );
        }

        public static string CreateUrl(string fileName, string offset, string length)
        {
            return string.Format(
                "{0}://{1}?offset={2}&length={3}",
                SCHEME,
                fileName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                offset,
                length
            );
        }

        public static bool ParseUrl(string url, out string fileName, out string offset, out string length)
        {
            return ParseUrl(new Uri(url), out fileName, out offset, out length);
        }

        public static bool ParseUrl(Uri url, out string fileName, out string offset, out string length)
        {
            fileName = default(string);
            offset = default(string);
            length = default(string);
            if (!string.Equals(url.Scheme, SCHEME, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            fileName = Uri.UnescapeDataString(
                url.AbsolutePath
            ).Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            offset = url.GetQueryParameter("offset");
            length = url.GetQueryParameter("length");
            return true;
        }
    }
}
