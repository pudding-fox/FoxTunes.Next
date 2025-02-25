using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FoxTunes
{
    public static class TestInfo
    {
        public static string CurrentDirectory
        {
            get
            {
                return Path.GetDirectoryName(typeof(TestInfo).Assembly.Location);
            }
        }

        public static string[] AudioFileNames
        {
            get
            {
                return new[]
                {
                    Path.Combine(CurrentDirectory, "Audio", "A.mp3"),
                    Path.Combine(CurrentDirectory, "Audio", "B.ogg"),
                    Path.Combine(CurrentDirectory, "Audio", "C.mp3"),
                    Path.Combine(CurrentDirectory, "Audio", "D.mp3"),
                    Path.Combine(CurrentDirectory, "Audio", "E.dsf")
                };
            }
        }

        public static PlaylistItem[] PlaylistItems
        {
            get
            {
                return AudioFileNames.Select(fileName => new PlaylistItem() { FileName = fileName, MetaDatas = new List<MetaDataItem>() }).ToArray();
            }
        }
    }
}
