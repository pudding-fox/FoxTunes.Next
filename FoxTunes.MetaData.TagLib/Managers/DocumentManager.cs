using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using TagLib;

namespace FoxTunes
{
    public static class DocumentManager
    {
        const string PREFIX = "document";

        const string MIME_TYPE_JSON = "application/json";

        static readonly Regex Base64 = new Regex("^([a-z0-9+/]{4})*([a-z0-9+/]{3}=|[a-z0-9+/]{2}==)?$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static void Read(TagLibMetaDataSource source, IList<MetaDataItem> metaDatas, File file)
        {
            try
            {
                if (file.InvariantStartPosition > (source.MaxTagSize.Value * 1024000))
                {
                    Logger.Write(typeof(ImageManager), LogLevel.Warn, "Not importing documents from file \"{0}\" due to size: {1} > {2}", file.Name, file.InvariantStartPosition, source.MaxTagSize.Value * 1024000);
                    return;
                }

                var pictures = file.Tag.Pictures;
                foreach (var picture in pictures)
                {
                    if (string.IsNullOrEmpty(picture.Description))
                    {
                        //We need the desciption for the meta data name.
                        continue;
                    }
                    try
                    {
                        if (string.Equals(picture.MimeType, MIME_TYPE_JSON, StringComparison.OrdinalIgnoreCase))
                        {
                            var name = string.Concat(PREFIX, ":", picture.Description);
                            var value = string.Concat(MIME_TYPE_JSON, ":", ReadJsonDocument(picture.Data.Data));
                            metaDatas.Add(new MetaDataItem(name, MetaDataItemType.Document)
                            {
                                Value = value
                            });
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Write(typeof(DocumentManager), LogLevel.Warn, "Failed to read document: {0} => {1} => {2}", file.Name, picture.Description, e.Message);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Write(typeof(DocumentManager), LogLevel.Warn, "Failed to read documents: {0} => {1}", file.Name, e.Message);
            }
        }

        private static string ReadJsonDocument(byte[] data)
        {
            var text = Encoding.UTF8.GetString(data);
            if (Base64.IsMatch(text))
            {
                text = Encoding.UTF8.GetString(Convert.FromBase64String(text));
            }
            return text;
        }

        public static void Write(TagLibMetaDataSource source, MetaDataItem metaDataItem, File file)
        {
            var index = default(int);
            var pictures = new List<IPicture>(file.Tag.Pictures);
            if (HasDocument(metaDataItem.Name, file.Tag, pictures, out index))
            {
                if (!string.IsNullOrEmpty(metaDataItem.Value))
                {
                    ReplaceDocument(metaDataItem, file, pictures, index);
                }
                else
                {
                    RemoveDocument(metaDataItem, file.Tag, pictures, index);
                }
            }
            else if (!string.IsNullOrEmpty(metaDataItem.Value))
            {
                AddDocument(metaDataItem, file, pictures);
            }
            file.Tag.Pictures = pictures.ToArray();
        }

        private static bool HasDocument(string name, Tag tag, IList<IPicture> pictures, out int index)
        {
            for (var a = 0; a < pictures.Count; a++)
            {
                if (pictures[a] != null && string.Equals(pictures[a].Description, name, StringComparison.OrdinalIgnoreCase))
                {
                    index = a;
                    return true;
                }
            }
            index = default(int);
            return false;
        }

        private static void AddDocument(MetaDataItem metaDataItem, File file, IList<IPicture> pictures)
        {
            pictures.Add(CreateDocument(metaDataItem, file));
        }

        private static void ReplaceDocument(MetaDataItem metaDataItem, File file, IList<IPicture> pictures, int index)
        {
            pictures[index] = CreateDocument(metaDataItem, file);
        }

        private static void RemoveDocument(MetaDataItem metaDataItem, Tag tag, IList<IPicture> pictures, int index)
        {
            pictures.RemoveAt(index);
        }

        private static IPicture CreateDocument(MetaDataItem metaDataItem, File file)
        {
            var parts = metaDataItem.Value.Split(new[] { ':' }, 2);
            if (parts.Length != 2)
            {
                Logger.Write(typeof(DocumentManager), LogLevel.Warn, "Failed to parse document: {0}", metaDataItem.Value);
                return null;
            }
            var mimeType = parts[0];
            var data = parts[1];
            var picture = new Picture(Encoding.UTF8.GetBytes(data))
            {
                Type = PictureType.NotAPicture,
                MimeType = mimeType
            };
            return picture;
        }
    }
}
