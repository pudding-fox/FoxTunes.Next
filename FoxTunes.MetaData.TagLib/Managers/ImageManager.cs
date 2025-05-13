using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TagLib;

namespace FoxTunes
{
    public static class ImageManager
    {
        private static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        private static readonly string PREFIX = typeof(TagLibMetaDataSource).Name;

        public static ArtworkType ArtworkTypes = ArtworkType.FrontCover | ArtworkType.BackCover | ArtworkType.Artist;

        public static readonly IDictionary<ArtworkType, PictureType> ArtworkTypeMapping = new Dictionary<ArtworkType, PictureType>()
        {
            { ArtworkType.FrontCover, PictureType.FrontCover },
            { ArtworkType.BackCover, PictureType.BackCover },
            { ArtworkType.Artist, PictureType.Artist }
        };

        public static readonly IDictionary<PictureType, ArtworkType> PrimaryPictureTypeMapping = new Dictionary<PictureType, ArtworkType>()
        {
            { PictureType.FrontCover, ArtworkType.FrontCover },
            { PictureType.BackCover, ArtworkType.BackCover },
            { PictureType.Artist, ArtworkType.Artist }
        };

        //Embedded images are sometimes misconfigured, try these if nothing else works.
        public static readonly IDictionary<PictureType, ArtworkType> SecondaryPictureTypeMapping = new Dictionary<PictureType, ArtworkType>()
        {
            { PictureType.Other, ArtworkType.FrontCover },
            { PictureType.NotAPicture, ArtworkType.FrontCover }
        };

        //TODO: Only use this pattern for UI code.
        public static readonly IArtworkProvider ArtworkProvider = ComponentRegistry.Instance.GetComponent<IArtworkProvider>();

        public static async Task<bool> Read(TagLibMetaDataSource source, IList<MetaDataItem> metaDatas, File file)
        {
            var embedded = source.EmbeddedImages.Value;
            var loose = source.LooseImages.Value;
            if (embedded && loose)
            {
                switch (MetaDataBehaviourConfiguration.GetImagesPreference(source.ImagesPreference.Value))
                {
                    default:
                    case ImagePreference.Embedded:
                        return await ReadEmbedded(source, metaDatas, file).ConfigureAwait(false) || await ReadLoose(source, metaDatas, file).ConfigureAwait(false);
                    case ImagePreference.Loose:
                        return await ReadLoose(source, metaDatas, file).ConfigureAwait(false) || await ReadEmbedded(source, metaDatas, file).ConfigureAwait(false);
                }
            }
            else if (embedded)
            {
                return await ReadEmbedded(source, metaDatas, file).ConfigureAwait(false);
            }
            else if (loose)
            {
                return await ReadLoose(source, metaDatas, file).ConfigureAwait(false);
            }
            return false;
        }

        private static async Task<bool> ReadEmbedded(TagLibMetaDataSource source, IList<MetaDataItem> metaDatas, File file)
        {
            var types = ArtworkType.None;
            try
            {
                if (file.InvariantStartPosition > source.MaxTagSize.Value * 1024000)
                {
                    Logger.Write(typeof(ImageManager), LogLevel.Warn, "Not importing images from file \"{0}\" due to size: {1} > {2}", file.Name, file.InvariantStartPosition, source.MaxTagSize.Value * 1024000);
                    return false;
                }

                var pictures = file.Tag.Pictures;
                if (pictures != null)
                {
                    foreach (var fallback in new[] { false, true })
                    {
                        foreach (var picture in pictures.OrderBy(picture => GetPicturePriority(picture)))
                        {
                            var type = GetArtworkType(picture.Type, fallback);
                            if (!ArtworkTypes.HasFlag(type) || types.HasFlag(type))
                            {
                                continue;
                            }

                            if (fallback)
                            {
                                if (!string.IsNullOrEmpty(picture.Description))
                                {
                                    //If we're in fallback (i.e the picture type isn't right) then ignore "pictures" with a description as it likely means the data has a specific purpose.
                                    Logger.Write(typeof(ImageManager), LogLevel.Warn, "Not importing image from file \"{0}\" due to description: {1}", file.Name, picture.Description);
                                    continue;
                                }
                                Logger.Write(typeof(ImageManager), LogLevel.Warn, "Importing image from file \"{0}\" with bad type: {1}.", file.Name, Enum.GetName(typeof(PictureType), picture.Type));
                                source.AddWarning(file.Name, string.Format("Image has bad type: {0}.", Enum.GetName(typeof(PictureType), picture.Type)));
                            }

                            if (string.IsNullOrEmpty(picture.MimeType))
                            {
                                Logger.Write(typeof(ImageManager), LogLevel.Warn, "Importing image from file \"{0}\" with empty mime type.", file.Name);
                                source.AddWarning(file.Name, "Image has empty mime type.");
                            }
                            else if (!MimeMapping.Instance.IsImage(picture.MimeType))
                            {
                                Logger.Write(typeof(ImageManager), LogLevel.Warn, "Importing image from file \"{0}\" with bad mime type: {1}", file.Name, picture.MimeType);
                                source.AddWarning(file.Name, string.Format("Image has bad mime type: {0}", picture.MimeType));
                            }

                            if (picture.Data.Count > source.MaxImageSize.Value * 1024000)
                            {
                                Logger.Write(typeof(ImageManager), LogLevel.Warn, "Not importing image from file \"{0}\" due to size: {1} > {2}", file.Name, picture.Data.Count, source.MaxImageSize.Value * 1024000);
                                source.AddWarning(file.Name, string.Format("Image was not imported due to size: {0} > {1}", picture.Data.Count, source.MaxImageSize.Value * 1024000));
                                continue;
                            }

                            metaDatas.Add(new MetaDataItem(Enum.GetName(typeof(ArtworkType), type), MetaDataItemType.Image)
                            {
                                Value = await ImportImage(file, picture, type, false).ConfigureAwait(false)
                            });
                            if (ArtworkTypes.HasFlag(types |= type))
                            {
                                //We have everything we need.
                                return true;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Write(typeof(ImageManager), LogLevel.Warn, "Failed to read pictures: {0} => {1}", file.Name, e.Message);
            }
            return types != ArtworkType.None;
        }

        private static async Task<bool> ReadLoose(TagLibMetaDataSource source, IList<MetaDataItem> metaDatas, File file)
        {
            var types = ArtworkType.None;
            try
            {
                foreach (var type in new[] { ArtworkType.FrontCover, ArtworkType.BackCover })
                {
                    if (!ArtworkTypes.HasFlag(type))
                    {
                        continue;
                    }
                    var value = ArtworkProvider.Find(file.Name, type);
                    if (!string.IsNullOrEmpty(value) && global::System.IO.File.Exists(value))
                    {
                        if (source.CopyImages.Value)
                        {
                            value = await ImportImage(value, value, false).ConfigureAwait(false);
                        }
                        metaDatas.Add(new MetaDataItem()
                        {
                            Name = Enum.GetName(typeof(ArtworkType), type),
                            Value = value,
                            Type = MetaDataItemType.Image
                        });
                        if (ArtworkTypes.HasFlag(types |= type))
                        {
                            //We have everything we need.
                            return true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Write(typeof(ImageManager), LogLevel.Warn, "Failed to read pictures: {0} => {1}", file.Name, e.Message);
            }
            return types != ArtworkType.None;
        }

        private static Task<string> ImportImage(File file, IPicture picture, ArtworkType type, bool overwrite)
        {
            var id = GetPictureId(file, picture, type);
            return ImportImage(picture, id, overwrite);
        }

        private static Task<string> ImportImage(IPicture value, string id, bool overwrite)
        {
            return FileMetaDataStore.IfNotExistsAsync(PREFIX, id, result => FileMetaDataStore.WriteAsync(PREFIX, id, value.Data.Data), overwrite);
        }

        private static Task<string> ImportImage(string fileName, string id, bool overwrite)
        {
            if (FileMetaDataStore.Contains(fileName))
            {
                //The file is already in the data store.
#if NET40
                return TaskEx.FromResult(fileName);
#else
                return Task.FromResult(fileName);
#endif
            }
            return FileMetaDataStore.IfNotExistsAsync(PREFIX, id, result => FileMetaDataStore.CopyAsync(PREFIX, id, fileName), overwrite);
        }

        public static async Task Write(TagLibMetaDataSource source, MetaDataItem metaDataItem, File file)
        {
            var embedded = source.EmbeddedImages.Value;
            var loose = source.LooseImages.Value;
            if (embedded && loose)
            {
                switch (MetaDataBehaviourConfiguration.GetImagesPreference(source.ImagesPreference.Value))
                {
                    default:
                    case ImagePreference.Embedded:
                        await WriteEmbedded(source, metaDataItem, file).ConfigureAwait(false);
                        break;
                    case ImagePreference.Loose:
                        WriteLoose(source, metaDataItem, file);
                        break;
                }
            }
            else if (embedded)
            {
                await WriteEmbedded(source, metaDataItem, file).ConfigureAwait(false);
            }
            else if (loose)
            {
                WriteLoose(source, metaDataItem, file);
            }
        }

        private static async Task WriteEmbedded(TagLibMetaDataSource source, MetaDataItem metaDataItem, File file)
        {
            var index = default(int);
            var pictures = new List<IPicture>(file.Tag.Pictures);
            if (HasImage(metaDataItem.Name, file.Tag, pictures, out index))
            {
                if (!string.IsNullOrEmpty(metaDataItem.Value))
                {
                    await ReplaceImage(metaDataItem, file, pictures, index).ConfigureAwait(false);
                }
                else
                {
                    RemoveImage(metaDataItem, file.Tag, pictures, index);
                }
            }
            else if (!string.IsNullOrEmpty(metaDataItem.Value))
            {
                await AddImage(metaDataItem, file, pictures).ConfigureAwait(false);
            }
            file.Tag.Pictures = pictures.ToArray();
        }

        private static void WriteLoose(TagLibMetaDataSource source, MetaDataItem metaDataItem, File file)
        {
            var fileName = default(string);
            if (HasImage(metaDataItem.Name, file, out fileName))
            {
                if (!string.IsNullOrEmpty(metaDataItem.Value))
                {
                    ReplaceImage(metaDataItem, fileName);
                }
                else
                {
                    RemoveImage(metaDataItem, fileName);
                }
            }
            else if (!string.IsNullOrEmpty(metaDataItem.Value))
            {
                AddImage(metaDataItem, file);
            }
        }

        private static bool HasImage(string name, Tag tag, IList<IPicture> pictures, out int index)
        {
            var type = GetArtworkType(name);
            for (var a = 0; a < pictures.Count; a++)
            {
                if (pictures[a] != null && GetArtworkType(pictures[a].Type, false) == type)
                {
                    index = a;
                    return true;
                }
            }
            index = default(int);
            return false;
        }

        private static bool HasImage(string name, File file, out string fileName)
        {
            var type = GetArtworkType(name);
            fileName = ArtworkProvider.Find(file.Name, type);
            return !string.IsNullOrEmpty(fileName) && global::System.IO.File.Exists(fileName);
        }

        private static async Task AddImage(MetaDataItem metaDataItem, File file, IList<IPicture> pictures)
        {
            pictures.Add(await CreateImage(metaDataItem, file).ConfigureAwait(false));
        }

        private static void AddImage(MetaDataItem metaDataItem, File file)
        {
            var type = GetArtworkType(metaDataItem.Name);
            //TODO: Assuming that the file name has the correct extension and not .bin
            var extension = global::System.IO.Path.GetExtension(metaDataItem.Value);
            var fileName = ArtworkProvider.GetFileName(file.Name, extension, type);
            global::System.IO.File.Copy(metaDataItem.Value, fileName, true);
            ArtworkProvider.Reset(file.Name, type);
        }

        private static async Task ReplaceImage(MetaDataItem metaDataItem, File file, IList<IPicture> pictures, int index)
        {
            pictures[index] = await CreateImage(metaDataItem, file).ConfigureAwait(false);
        }

        private static void ReplaceImage(MetaDataItem metaDataItem, string fileName)
        {
            var type = GetArtworkType(metaDataItem.Name);
            global::System.IO.File.Copy(metaDataItem.Value, fileName, true);
            ArtworkProvider.Reset(fileName, type);
        }

        private static void RemoveImage(MetaDataItem metaDataItem, Tag tag, IList<IPicture> pictures, int index)
        {
            pictures.RemoveAt(index);
        }

        private static void RemoveImage(MetaDataItem metaDataItem, string fileName)
        {
            var type = GetArtworkType(metaDataItem.Name);
            global::System.IO.File.Delete(fileName);
            ArtworkProvider.Reset(fileName, type);
        }

        private static async Task<IPicture> CreateImage(MetaDataItem metaDataItem, File file)
        {
            var type = GetArtworkType(metaDataItem.Name);
            var picture = new Picture(metaDataItem.Value)
            {
                Type = GetPictureType(type),
                //TODO: Assuming that the file name has the correct extension and not .bin
                MimeType = MimeMapping.Instance.GetMimeType(metaDataItem.Value)
            };
            metaDataItem.Value = await ImportImage(file, picture, type, true).ConfigureAwait(false);
            return picture;
        }

        private static string GetPictureId(File file, IPicture picture, ArtworkType type)
        {
            //Year + (Album | Checksum) + Type
            var hashCode = default(long);
            unchecked
            {
                if (file.Tag.Year != 0)
                {
                    hashCode = (hashCode * 29) + file.Tag.Year.GetHashCode();
                }
                if (!string.IsNullOrEmpty(file.Tag.Album))
                {
                    hashCode += file.Tag.Album.ToLower().GetDeterministicHashCode();
                }
                else
                {
                    hashCode += picture.Data.Checksum;
                }
                hashCode = (hashCode * 29) + type.GetHashCode();
            }
            return Math.Abs(hashCode).ToString();
        }

        private static PictureType GetPictureType(ArtworkType artworkType)
        {
            var pictureType = default(PictureType);
            if (ArtworkTypeMapping.TryGetValue(artworkType, out pictureType))
            {
                return pictureType;
            }
            return PictureType.NotAPicture;
        }

        private static byte GetPicturePriority(IPicture picture)
        {
            switch (picture.Type)
            {
                //Prefer covers.
                case PictureType.FrontCover:
                case PictureType.BackCover:
                    return 0;
            }
            if (!string.IsNullOrEmpty(picture.MimeType))
            {
                if (picture.MimeType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                {
                    //Then images.
                    return 100;
                }
            }
            //Everything else.
            return 255;
        }

        private static ArtworkType GetArtworkType(string name)
        {
            var artworkType = default(ArtworkType);
            if (Enum.TryParse<ArtworkType>(name, out artworkType))
            {
                return artworkType;
            }
            return ArtworkType.Unknown;
        }

        private static ArtworkType GetArtworkType(PictureType pictureType, bool fallback)
        {
            var artworkType = default(ArtworkType);
            if (fallback)
            {
                if (SecondaryPictureTypeMapping.TryGetValue(pictureType, out artworkType))
                {
                    return artworkType;
                }
            }
            if (PrimaryPictureTypeMapping.TryGetValue(pictureType, out artworkType))
            {
                return artworkType;
            }
            return ArtworkType.Unknown;
        }
    }
}
