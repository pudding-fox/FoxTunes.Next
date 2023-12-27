using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace FoxTunes
{
    public class PlaylistConfig : Dictionary<string, string>
    {
        public PlaylistConfig(Playlist playlist) : base(StringComparer.OrdinalIgnoreCase)
        {
            this.Playlist = playlist;
            this.Load();
        }

        public Playlist Playlist { get; private set; }

        public void Load()
        {
            this.Clear();
            if (string.IsNullOrEmpty(this.Playlist.Config))
            {
                return;
            }
            foreach (var pair in Serializer.Load(this.Playlist.Config))
            {
                this[pair.Key] = pair.Value;
            }
        }

        public void Save()
        {
            this.Playlist.Config = Serializer.Save(this);
        }

        public static class Serializer
        {
            const string Key = "Key";

            public static IEnumerable<KeyValuePair<string, string>> Load(string config)
            {
                using (var stream = config.ToStream())
                {
                    return Load(stream).ToArray();
                }
            }

            public static IEnumerable<KeyValuePair<string, string>> Load(Stream stream)
            {
                using (var reader = new XmlTextReader(stream))
                {
                    reader.ReadStartElement(Publication.Product);
                    while (reader.IsStartElement(nameof(PlaylistConfig)))
                    {
                        var key = reader.GetAttribute(Key);
                        var value = reader.ReadElementContentAsString();
                        if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                        {
                            yield return new KeyValuePair<string, string>(key, value);
                        }
                        if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, nameof(PlaylistConfig)))
                        {
                            reader.ReadEndElement();
                        }
                    }
                    if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, Publication.Product))
                    {
                        reader.ReadEndElement();
                    }
                }
            }

            public static string Save(IEnumerable<KeyValuePair<string, string>> sequence)
            {
                using (var stream = new MemoryStream())
                {
                    Save(stream, sequence);
                    return Encoding.Default.GetString(stream.ToArray());
                }
            }

            public static void Save(Stream stream, IEnumerable<KeyValuePair<string, string>> sequence)
            {
                using (var writer = new XmlTextWriter(stream, Encoding.Default))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement(Publication.Product);
                    foreach (var pair in sequence)
                    {
                        if (string.IsNullOrEmpty(pair.Key) || string.IsNullOrEmpty(pair.Value))
                        {
                            continue;
                        }
                        writer.WriteStartElement(nameof(PlaylistConfig));
                        writer.WriteAttributeString(Key, pair.Key);
                        writer.WriteValue(pair.Value);
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }
        }
    }
}
