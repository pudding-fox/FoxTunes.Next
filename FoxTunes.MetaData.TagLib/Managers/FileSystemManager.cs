using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using TagLib;

namespace FoxTunes
{
    public class FileSystemManager
    {
        public static void Read(TagLibMetaDataSource source, IList<MetaDataItem> metaData, File file)
        {
            if (file.FileAbstraction is ITagLibFileAbstraction fileAbstraction)
            {
                Read(source, metaData, fileAbstraction.FileAbstraction);
            }
            else
            {
                Read(source, metaData, file.Name);
            }
        }

        public static void Read(TagLibMetaDataSource source, IList<MetaDataItem> metaData, IFileAbstraction fileAbstraction)
        {
            source.AddTag(metaData, FileSystemProperties.FileName, fileAbstraction.FileName);
            source.AddTag(metaData, FileSystemProperties.DirectoryName, fileAbstraction.DirectoryName);
            source.AddTag(metaData, FileSystemProperties.FileExtension, fileAbstraction.FileExtension);
            source.AddTag(metaData, FileSystemProperties.FileSize, Convert.ToString(fileAbstraction.FileSize));
            source.AddTag(metaData, FileSystemProperties.FileCreationTime, DateTimeHelper.ToString(fileAbstraction.FileCreationTime));
            source.AddTag(metaData, FileSystemProperties.FileModificationTime, DateTimeHelper.ToString(fileAbstraction.FileModificationTime));
        }

        public static void Read(TagLibMetaDataSource source, IList<MetaDataItem> metaData, string fileName)
        {
            var info = new global::System.IO.FileInfo(fileName);
            if (!info.Exists)
            {
                return;
            }
            source.AddTag(metaData, FileSystemProperties.FileName, info.Name);
            source.AddTag(metaData, FileSystemProperties.DirectoryName, info.DirectoryName);
            source.AddTag(metaData, FileSystemProperties.FileExtension, info.Extension);
            source.AddTag(metaData, FileSystemProperties.FileSize, Convert.ToString(info.Length));
            source.AddTag(metaData, FileSystemProperties.FileCreationTime, DateTimeHelper.ToString(info.CreationTime));
            source.AddTag(metaData, FileSystemProperties.FileModificationTime, DateTimeHelper.ToString(info.LastWriteTime));
        }

        public static void Write(TagLibMetaDataSource source, MetaDataItem metaDataItem, File file)
        {
            //Nothing to do.
        }
    }
}
