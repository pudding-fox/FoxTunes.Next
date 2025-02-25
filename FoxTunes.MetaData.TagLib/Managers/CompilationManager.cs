using System;
using System.Collections.Generic;
using TagLib;

namespace FoxTunes
{
    public static class CompilationManager
    {
        public static void Read(TagLibMetaDataSource source, IList<MetaDataItem> metaData, File file)
        {
            var isCompilation = default(bool);

            if (TagManager.HasTag(file, TagTypes.Id3v2))
            {
                var tag = TagManager.GetTag<global::TagLib.Id3v2.Tag>(file, TagTypes.Id3v2);
                if (tag != null)
                {
                    isCompilation = tag.IsCompilation;
                }
            }
            else if (TagManager.HasTag(file, TagTypes.Apple))
            {
                var tag = TagManager.GetTag<global::TagLib.Mpeg4.AppleTag>(file, TagTypes.Apple);
                if (tag != null)
                {
                    isCompilation = tag.IsCompilation;
                }
            }
            else if (TagManager.HasTag(file, TagTypes.Xiph))
            {
                var tag = TagManager.GetTag<global::TagLib.Ogg.XiphComment>(file, TagTypes.Xiph);
                if (tag != null)
                {
                    isCompilation = tag.IsCompilation;
                }
            }

            //Check MB release type, it's innocuous so don't bother respecting READ_MUSICBRAINZ_TAGS.
            if (string.Equals(file.Tag.MusicBrainzReleaseType, MusicBrainzReleaseType.Compilation, StringComparison.OrdinalIgnoreCase))
            {
                isCompilation = true;
            }

            if (isCompilation)
            {
                source.AddTag(metaData, CommonMetaData.IsCompilation, bool.TrueString);
                //TODO: CustomMetaData.VariousArtists should go away but scripts use it, let's keep it updated for now.
                source.AddTag(metaData, CustomMetaData.VariousArtists, bool.TrueString);
            }
        }

        public static void Write(TagLibMetaDataSource source, MetaDataItem metaDataItem, File file)
        {
            if (string.Equals(metaDataItem.Name, CommonMetaData.IsCompilation, StringComparison.OrdinalIgnoreCase))
            {
                var isCompilation = string.Equals(metaDataItem.Value, bool.TrueString, StringComparison.OrdinalIgnoreCase);

                if (TagManager.HasTag(file, TagTypes.Id3v2))
                {
                    var tag = TagManager.GetTag<global::TagLib.Id3v2.Tag>(file, TagTypes.Id3v2);
                    if (tag != null)
                    {
                        tag.IsCompilation = isCompilation;
                    }
                }
                else if (TagManager.HasTag(file, TagTypes.Apple))
                {
                    var tag = TagManager.GetTag<global::TagLib.Mpeg4.AppleTag>(file, TagTypes.Apple);
                    if (tag != null)
                    {
                        tag.IsCompilation = isCompilation;
                    }
                }
                else if (TagManager.HasTag(file, TagTypes.Xiph))
                {
                    var tag = TagManager.GetTag<global::TagLib.Ogg.XiphComment>(file, TagTypes.Xiph);
                    if (tag != null)
                    {
                        tag.IsCompilation = isCompilation;
                    }
                }

                if (source.MusicBrainz.Value)
                {
                    if (isCompilation)
                    {
                        file.Tag.MusicBrainzReleaseType = MusicBrainzReleaseType.Compilation;
                    }
                    else if (string.Equals(file.Tag.MusicBrainzReleaseType, MusicBrainzReleaseType.Compilation, StringComparison.OrdinalIgnoreCase))
                    {
                        //TODO: MusicBrainzReleaseType could be anything...
                    }
                }
            }
        }
    }
}
