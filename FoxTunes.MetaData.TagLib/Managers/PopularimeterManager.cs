using System;
using System.Collections.Generic;
using System.Linq;
using TagLib;

namespace FoxTunes
{
    public static class PopularimeterManager
    {
        public static void Read(TagLibMetaDataSource source, IList<MetaDataItem> metaData, File file)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            //If it's an Id3v2 tag then try to read the popularimeter frame.
            //It can contain a rating and a play counter.
            if (file.TagTypes.HasFlag(TagTypes.Id3v2))
            {
                var tag = TagManager.GetTag<global::TagLib.Id3v2.Tag>(file, TagTypes.Id3v2);
                if (tag == null)
                {
                    return;
                }
                foreach (var frame in tag.GetFrames<global::TagLib.Id3v2.PopularimeterFrame>())
                {
                    ReadPopularimeterFrame(frame, result);
                }
            }
            //If we didn't find a popularimeter frame then attempt to read the rating from a custom tag.
            if (!result.ContainsKey(CommonStatistics.Rating))
            {
                var rating = TagManager.ReadCustomTag(CommonStatistics.Rating, file);
                if (!string.IsNullOrEmpty(rating))
                {
                    result.Add(CommonStatistics.Rating, Convert.ToString(GetRatingStars(rating)));
                }
                else
                {
                    result.Add(CommonStatistics.Rating, string.Empty);
                }
            }
            //If we didn't find a popularimeter frame then attempt to read the play count from a custom tag.
            if (!result.ContainsKey(CommonStatistics.PlayCount))
            {
                var playCount = TagManager.ReadCustomTag(CommonStatistics.PlayCount, file);
                if (!string.IsNullOrEmpty(playCount))
                {
                    result.Add(CommonStatistics.PlayCount, playCount);
                }
                else
                {
                    result.Add(CommonStatistics.PlayCount, "0");
                }
            }
            //Popularimeter frame does not support last played, attempt to read the play count from a custom tag.
            //if (!result.ContainsKey(CommonMetaData.LastPlayed))
            {
                var lastPlayed = TagManager.ReadCustomTag(CommonStatistics.LastPlayed, file);
                if (!string.IsNullOrEmpty(lastPlayed))
                {
                    result.Add(CommonStatistics.LastPlayed, lastPlayed);
                }
                else
                {
                    result.Add(CommonStatistics.LastPlayed, DateTimeHelper.NEVER);
                }
            }
            //Copy our informations back to the meta data collection.
            foreach (var key in result.Keys)
            {
                var value = result[key];
                source.AddTag(metaData, key, value);
            }
        }

        private static void ReadPopularimeterFrame(global::TagLib.Id3v2.PopularimeterFrame frame, IDictionary<string, string> result)
        {
            if (frame.Rating > 0)
            {
                result.Add(CommonStatistics.Rating, Convert.ToString(GetRatingStars(frame.Rating)));
            }
            if (frame.PlayCount > 0)
            {
                result.Add(CommonStatistics.PlayCount, Convert.ToString(frame.PlayCount));
            }
        }

        public static void Write(TagLibMetaDataSource source, MetaDataItem metaDataItem, File file)
        {
            if (file.TagTypes.HasFlag(TagTypes.Id3v2) && (new[] { CommonStatistics.Rating, CommonStatistics.PlayCount }).Contains(metaDataItem.Name, StringComparer.OrdinalIgnoreCase))
            {
                var tag = TagManager.GetTag<global::TagLib.Id3v2.Tag>(file, TagTypes.Id3v2);
                var frames = tag.GetFrames<global::TagLib.Id3v2.PopularimeterFrame>();
                if (frames != null && frames.Any())
                {
                    foreach (var frame in frames)
                    {
                        WritePopularimeterFrame(frame, metaDataItem);
                    }
                }
                else
                {
                    var frame = new global::TagLib.Id3v2.PopularimeterFrame(string.Empty);
                    WritePopularimeterFrame(frame, metaDataItem);
                    tag.AddFrame(frame);
                }
            }
            else if (string.Equals(metaDataItem.Name, CommonStatistics.Rating, StringComparison.OrdinalIgnoreCase))
            {
                TagManager.WriteCustomTag(metaDataItem.Name, Convert.ToString(GetRatingMask(metaDataItem.Value)), file);
            }
            else
            {
                TagManager.WriteCustomTag(metaDataItem.Name, metaDataItem.Value, file);
            }
        }

        private static void WritePopularimeterFrame(global::TagLib.Id3v2.PopularimeterFrame frame, MetaDataItem metaDataItem)
        {
            if (string.Equals(metaDataItem.Name, CommonStatistics.Rating, StringComparison.OrdinalIgnoreCase))
            {
                frame.Rating = GetRatingMask(metaDataItem.Value);
            }
            else if (string.Equals(metaDataItem.Name, CommonStatistics.PlayCount, StringComparison.OrdinalIgnoreCase))
            {
                frame.PlayCount = Convert.ToUInt64(metaDataItem.Value);
            }
        }

        private static byte GetRatingMask(string rating)
        {
            var temp = default(byte);
            if (!byte.TryParse(rating, out temp))
            {
                return 0;
            }
            return GetRatingMask(temp);
        }

        private static byte GetRatingMask(byte rating)
        {
            //TODO: We only write WMP style ratings but this should be configurable.
            const byte RATING_0 = 0;
            const byte RATING_1 = 1;
            const byte RATING_2 = 64;
            const byte RATING_3 = 128;
            const byte RATING_4 = 196;
            const byte RATING_5 = 255;

            switch (rating)
            {
                default:
                case 0:
                    return RATING_0;
                case 1:
                    return RATING_1;
                case 2:
                    return RATING_2;
                case 3:
                    return RATING_3;
                case 4:
                    return RATING_4;
                case 5:
                    return RATING_5;
            }
        }

        private static byte GetRatingStars(string rating)
        {
            var temp = default(byte);
            if (!byte.TryParse(rating, out temp))
            {
                return 0;
            }
            return GetRatingStars(temp);
        }

        private static byte GetRatingStars(byte rating)
        {
            //There are no solid definitions of what 0-255 rating maps to 0-5 stars.
            //This logic was adapted various sources.
            //iTunes ratings are 0-100 but there's no good way to identify them as such.

            const byte RATING_0 = 0;
            const byte RATING_1 = 1;
            const byte RATING_2 = 2;
            const byte RATING_3 = 3;
            const byte RATING_4 = 4;
            const byte RATING_5 = 5;

            //First try the simple approach.
            switch (rating)
            {
                case 0:
                    return RATING_0;
                case 1:
                    return RATING_1;
                case 64:
                    return RATING_2;
                case 128:
                    return RATING_3;
                case 196:
                    return RATING_4;
                case 255:
                    return RATING_5;
            }
            //Then try other things.
            if (rating >= 2 && rating <= 8)
            {
                return RATING_0;
            }
            else if (rating >= 9 && rating <= 18)
            {
                //0.5 rounded up.
                return RATING_1;
            }
            else if (rating >= 19 && rating <= 28)
            {
                return RATING_1;
            }
            else if (rating == 29)
            {
                //1.5 rounded up.
                return RATING_2;
            }
            else if (rating >= 30 && rating <= 39)
            {
                //0.5 rounded up.
                return RATING_1;
            }
            else if (rating >= 40 && rating <= 49)
            {
                return RATING_1;
            }
            else if (rating >= 50 && rating <= 59)
            {
                //1.5 rounded up.
                return RATING_2;
            }
            else if (rating >= 60 && rating <= 69)
            {
                return RATING_2;
            }
            else if (rating >= 70 && rating <= 90)
            {
                //1.5 rounded up.
                return RATING_2;
            }
            else if (rating >= 91 && rating <= 113)
            {
                return RATING_2;
            }
            else if (rating >= 114 && rating <= 123)
            {
                //2.5 rounded up.
                return RATING_3;
            }
            else if (rating >= 124 && rating <= 133)
            {
                return RATING_3;
            }
            else if (rating >= 134 && rating <= 141)
            {
                //2.5 rounded up.
                return RATING_3;
            }
            else if (rating >= 142 && rating <= 167)
            {
                return RATING_3;
            }
            else if (rating >= 168 && rating <= 191)
            {
                //3.5 rounded up.
                return RATING_4;
            }
            else if (rating >= 192 && rating <= 218)
            {
                return RATING_4;
            }
            else if (rating >= 219 && rating <= 247)
            {
                //4.5 rounded up.
                return RATING_5;
            }

            return RATING_5;
        }
    }
}
