using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace FoxTunes
{
    public class M3UHelper
    {
        public class Reader
        {
            public Reader(IEnumerable<string> content)
            {
                this.Content = content.ToList();
            }

            public IList<string> Content { get; private set; }

            public IEnumerable<PlaylistItem> Read()
            {
                var playlistItem = default(PlaylistItem);
                var followLinks = true;
                for (var a = 0; a < this.Content.Count; a++)
                {
                    var line = this.Content[a];
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }
                    else
                    {
                        line = line.Trim();
                    }
                    if (line.StartsWith("#EXTINF", StringComparison.OrdinalIgnoreCase))
                    {
                        playlistItem = this.Read(line);
                    }
                    else if (!line.StartsWith("#"))
                    {
                        var m3uSuffixes = new[]
                        {
                            "m3u",
                            "m3u?",
                            "m3u8",
                            "m3u8?"
                        };
                        if (m3uSuffixes.Any(m3uSuffix => line.EndsWith(m3uSuffix, StringComparison.OrdinalIgnoreCase)))
                        {
                            if (followLinks)
                            {
                                this.Content.AddRange(Fetch(line));
                                followLinks = false;
                            }
                            continue;
                        }
                        if (playlistItem == null)
                        {
                            playlistItem = new PlaylistItem();
                        }
                        playlistItem.DirectoryName = line;
                        playlistItem.FileName = line;
                        yield return playlistItem;
                        playlistItem = null;
                    }
                }
            }

            public PlaylistItem Read(string line)
            {
                var playlistItem = new PlaylistItem()
                {
                    MetaDatas = new List<MetaDataItem>()
                };
                var matches = Regex.Matches(line, "(\\w+?)=\"([^\"]+)\"");
                foreach (Match match in matches)
                {
                    var metaDataItem = new MetaDataItem()
                    {
                        Name = match.Groups[1].Value,
                        Type = MetaDataItemType.Tag,
                        Value = match.Groups[2].Value
                    };
                    playlistItem.MetaDatas.Add(metaDataItem);
                }
                return playlistItem;
            }
        }

        public static Reader FromUrl(string url)
        {
            return new Reader(Fetch(url));
        }

        public static IEnumerable<string> Fetch(string url)
        {
            using (var client = new WebClient())
            {
                var content = client.DownloadString(url);
                if (string.IsNullOrEmpty(content))
                {
                    return Enumerable.Empty<string>();
                }
                return content.Split('\r', '\n');
            }
        }
    }
}
