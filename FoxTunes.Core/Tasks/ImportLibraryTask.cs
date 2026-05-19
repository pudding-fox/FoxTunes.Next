using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Xml;

namespace FoxTunes
{
    public class ImportLibraryTask : BackgroundTask
    {
        public const string ID = "AF880895-7878-4B01-AE7C-E8BBA59C5703";

        public ImportLibraryTask(string fileName) : base(ID)
        {
            this.FileName = fileName;
        }

        public override bool Visible
        {
            get
            {
                return true;
            }
        }

        public override bool Cancellable
        {
            get
            {
                return true;
            }
        }

        public string FileName { get; private set; }

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public ISignalEmitter SignalEmitter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.DatabaseFactory = core.Factories.Database;
            this.SignalEmitter = core.Components.SignalEmitter;
            base.InitializeComponent(core);
        }

        protected override async Task OnRun()
        {
            this.Name = "Importing..";
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    using (var libraryWriter = new LibraryWriter(database, transaction))
                    {
                        using (var metaDataWriter = new MetaDataWriter(database, database.Queries.AddLibraryMetaDataItem, transaction))
                        {
                            using (var fileStream = File.OpenRead(this.FileName))
                            {
                                using (var zipStream = new GZipStream(fileStream, CompressionMode.Decompress))
                                {
                                    foreach (var libraryItem in Serializer.Load(zipStream))
                                    {
                                        if (this.IsCancellationRequested)
                                        {
                                            break;
                                        }
                                        await this.Import(libraryWriter, metaDataWriter, libraryItem).ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                    }
                    transaction.Commit();
                }
            }
        }

        protected virtual async Task Import(LibraryWriter libraryWriter, MetaDataWriter metaDataWriter, LibraryItem libraryItem)
        {
            Logger.Write(this, LogLevel.Debug, "Importing: {0}", libraryItem.FileName);
            this.Name = Path.GetFileName(libraryItem.FileName);
            await libraryWriter.Write(libraryItem).ConfigureAwait(false);
            foreach (var metaDataItem in libraryItem.MetaDatas)
            {
                metaDataItem.Value = this.Import(metaDataItem.Value);
                await metaDataWriter.Write(libraryItem.Id, metaDataItem).ConfigureAwait(false);
            }
        }

        protected virtual string Import(string value)
        {
            try
            {
                if (string.IsNullOrEmpty(value))
                {
                    return value;
                }
                if (!value.StartsWith("DATA{", StringComparison.OrdinalIgnoreCase))
                {
                    return value;
                }
                var offset = value.IndexOf("}=", StringComparison.OrdinalIgnoreCase);
                if (offset == -1)
                {
                    return value;
                }
                var fileName = value.Substring(5, offset - 5);
                Logger.Write(this, LogLevel.Debug, "Importing: {0}", fileName);
                var data = value.Substring(offset + 2);
                var bytes = Convert.FromBase64String(data);
                var directoryName = Path.GetDirectoryName(fileName);
                if (!string.IsNullOrEmpty(directoryName))
                {
                    Directory.CreateDirectory(directoryName);
                }
                File.WriteAllBytes(fileName, bytes);
                return fileName;
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to import embedded data: {0}", e.Message);
                return string.Empty;
            }
        }

        protected override async Task OnCompleted()
        {
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.LibraryUpdated)).ConfigureAwait(false);
            await base.OnCompleted().ConfigureAwait(false);
        }

        public class Serializer
        {
            public static IEnumerable<LibraryItem> Load(Stream stream)
            {
                using (var reader = new XmlTextReader(stream))
                {
                    if (reader.IsStartElement(Publication.Product))
                    {
                        reader.ReadStartElement(Publication.Product);
                    }
                    while (reader.IsStartElement(nameof(LibraryItem)))
                    {
                        yield return LoadLibraryItem(reader);
                        if (reader.NodeType == XmlNodeType.EndElement && string.Equals(reader.Name, nameof(LibraryItem)))
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

            private static LibraryItem LoadLibraryItem(XmlReader reader)
            {
                var libraryItem = new LibraryItem();
                libraryItem.DirectoryName = reader.GetAttribute(nameof(LibraryItem.DirectoryName));
                libraryItem.FileName = reader.GetAttribute(nameof(LibraryItem.FileName));
                libraryItem.ImportDate = reader.GetAttribute(nameof(LibraryItem.ImportDate));
                libraryItem.Status = (LibraryItemStatus)Enum.Parse(typeof(LibraryItemStatus), reader.GetAttribute(nameof(LibraryItem.Status)));
                libraryItem.Flags = (LibraryItemFlags)Enum.Parse(typeof(LibraryItemFlags), reader.GetAttribute(nameof(LibraryItem.Flags)));
                libraryItem.MetaDatas = new List<MetaDataItem>();
                reader.ReadStartElement(nameof(LibraryItem));
                while (reader.IsStartElement(nameof(MetaDataItem)))
                {
                    var metaDataItem = new MetaDataItem();
                    metaDataItem.Name = reader.GetAttribute(nameof(MetaDataItem.Name));
                    metaDataItem.Type = (MetaDataItemType)Enum.Parse(typeof(MetaDataItemType), reader.GetAttribute(nameof(MetaDataItem.Type)));
                    metaDataItem.Value = reader.ReadElementContentAsString();
                    libraryItem.MetaDatas.Add(metaDataItem);
                }
                return libraryItem;
            }
        }
    }
}
