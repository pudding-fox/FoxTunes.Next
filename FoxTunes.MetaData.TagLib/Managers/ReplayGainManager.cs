using System;
using System.Collections.Generic;
using TagLib;

namespace FoxTunes
{
    public static class ReplayGainManager
    {
        public static void Read(TagLibMetaDataSource source, IList<MetaDataItem> metaData, File file)
        {
            var tag = file.Tag;
            if (tag.ReplayGainAlbumPeak != 0 && !double.IsNaN(tag.ReplayGainAlbumPeak) && !double.IsInfinity(tag.ReplayGainAlbumPeak))
            {
                source.AddTag(metaData, CommonMetaData.ReplayGainAlbumPeak, tag.ReplayGainAlbumPeak.ToString());
            }
            if (tag.ReplayGainAlbumGain != 0 && !double.IsNaN(tag.ReplayGainAlbumGain) && !double.IsInfinity(tag.ReplayGainAlbumGain))
            {
                source.AddTag(metaData, CommonMetaData.ReplayGainAlbumGain, tag.ReplayGainAlbumGain.ToString());
            }
            if (tag.ReplayGainTrackPeak != 0 && !double.IsNaN(tag.ReplayGainTrackPeak) && !double.IsInfinity(tag.ReplayGainTrackPeak))
            {
                source.AddTag(metaData, CommonMetaData.ReplayGainTrackPeak, tag.ReplayGainTrackPeak.ToString());
            }
            if (tag.ReplayGainTrackGain != 0 && !double.IsNaN(tag.ReplayGainTrackGain) && !double.IsInfinity(tag.ReplayGainTrackGain))
            {
                source.AddTag(metaData, CommonMetaData.ReplayGainTrackGain, tag.ReplayGainTrackGain.ToString());
            }
        }

        public static void Write(TagLibMetaDataSource source, MetaDataItem metaDataItem, File file)
        {
            var tag = file.Tag;
            var value = default(double);
            if (string.IsNullOrEmpty(metaDataItem.Value))
            {
                value = double.NaN;
            }
            else if (!double.TryParse(metaDataItem.Value, out value))
            {
                value = double.NaN;
            }
            if (string.Equals(metaDataItem.Name, CommonMetaData.ReplayGainAlbumGain, StringComparison.OrdinalIgnoreCase))
            {
                tag.ReplayGainAlbumGain = value;
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.ReplayGainAlbumPeak, StringComparison.OrdinalIgnoreCase))
            {
                tag.ReplayGainAlbumPeak = value;
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.ReplayGainTrackGain, StringComparison.OrdinalIgnoreCase))
            {
                tag.ReplayGainTrackGain = value;
            }
            else if (string.Equals(metaDataItem.Name, CommonMetaData.ReplayGainTrackPeak, StringComparison.OrdinalIgnoreCase))
            {
                tag.ReplayGainTrackPeak = value;
            }
        }
    }
}
