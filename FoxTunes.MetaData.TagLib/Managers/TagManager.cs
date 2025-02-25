using System;
using System.Linq;
using System.Text;
using TagLib;

namespace FoxTunes
{
    public static class TagManager
    {
        public static bool HasTag(File file, TagTypes tagTypes)
        {
            return file.TagTypes.HasFlag(tagTypes);
        }

        public static T GetTag<T>(File file, TagTypes tagTypes) where T : Tag
        {
            return file.GetTag(tagTypes) as T;
        }

        public static string ReadCustomTag(string name, File file)
        {
            var key = GetCustomTagName(name);
            if (file.TagTypes.HasFlag(TagTypes.Id3v2))
            {
                var tag = GetTag<global::TagLib.Id3v2.Tag>(file, TagTypes.Id3v2);
                if (tag != null)
                {
                    var frame = global::TagLib.Id3v2.PrivateFrame.Get(tag, key, false);
                    if (frame != null && frame.PrivateData != null && frame.PrivateData.Count > 0)
                    {
                        try
                        {
                            //We're sort of sure that it's UTF-16.
                            return Encoding.Unicode.GetString(frame.PrivateData.Data);
                        }
                        catch
                        {
                            //Nothing can be done, wasn't in the expected format.
                        }
                    }
                }
            }
            else if (file.TagTypes.HasFlag(TagTypes.Apple))
            {
                var tag = GetTag<global::TagLib.Mpeg4.AppleTag>(file, TagTypes.Apple);
                if (tag != null)
                {
                    return tag.GetDashBox("com.apple.iTunes", key);
                }
            }
            else if (file.TagTypes.HasFlag(TagTypes.Xiph))
            {
                var tag = GetTag<global::TagLib.Ogg.XiphComment>(file, TagTypes.Xiph);
                if (tag != null)
                {
                    return tag.GetFirstField(key);
                }
            }
            else if (file.TagTypes.HasFlag(TagTypes.Ape))
            {
                var tag = GetTag<global::TagLib.Ape.Tag>(file, TagTypes.Ape);
                if (tag != null)
                {
                    var item = tag.GetItem(key);
                    if (item != null)
                    {
                        return item.ToStringArray().FirstOrDefault();
                    }
                }
            }
            //Not implemented.
            return null;
        }

        public static void WriteCustomTag(string name, string value, File file)
        {
            var key = GetCustomTagName(name);
            if (file.TagTypes.HasFlag(TagTypes.Id3v2))
            {
                var tag = GetTag<global::TagLib.Id3v2.Tag>(file, TagTypes.Id3v2);
                if (tag != null)
                {
                    var frame = global::TagLib.Id3v2.PrivateFrame.Get(tag, key, true);
                    //We're sort of sure that it's UTF-16.
                    frame.PrivateData = Encoding.Unicode.GetBytes(value);
                }
            }
            else if (file.TagTypes.HasFlag(TagTypes.Apple))
            {
                const string PREFIX = "\0\0\0\0";
                const string MEAN = "com.apple.iTunes";
                var apple = GetTag<global::TagLib.Mpeg4.AppleTag>(file, TagTypes.Apple);
                if (apple != null)
                {
                    while (apple.GetDashBox(MEAN, key) != null)
                    {
                        apple.SetDashBox(MEAN, key, string.Empty);
                    }
                    apple.SetDashBox(PREFIX + MEAN, PREFIX + key, value);
                }
            }
            else if (file.TagTypes.HasFlag(TagTypes.Xiph))
            {
                var xiph = GetTag<global::TagLib.Ogg.XiphComment>(file, TagTypes.Xiph);
                if (xiph != null)
                {
                    xiph.SetField(key, new[] { value });
                }
            }
            else if (file.TagTypes.HasFlag(TagTypes.Ape))
            {
                var ape = GetTag<global::TagLib.Ape.Tag>(file, TagTypes.Ape);
                if (ape != null)
                {
                    ape.SetValue(key, value);
                }
            }
            //Not implemented.
        }

        private static string GetCustomTagName(string name)
        {
            if (string.Equals(name, CommonStatistics.LastPlayed, StringComparison.OrdinalIgnoreCase))
            {
                //TODO: I can't work out what the standard is for this value.
                return "last_played_timestamp";
            }
            else if (string.Equals(name, CommonStatistics.PlayCount, StringComparison.OrdinalIgnoreCase))
            {
                return "play_count";
            }
            return name.ToLower();
        }
    }
}
