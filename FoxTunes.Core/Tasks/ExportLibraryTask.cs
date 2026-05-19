using FoxDb;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FoxTunes
{
    public class ExportLibraryTask : BackgroundTask
    {
        public const string ID = "D11C5ED3-A2C7-461B-88CC-2EE9D53D5CF8";

        public ExportLibraryTask(string fileName) : base(ID)
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

        public override void InitializeComponent(ICore core)
        {
            this.DatabaseFactory = core.Factories.Database;
            base.InitializeComponent(core);
        }

        protected override Task OnRun()
        {
            this.Name = "Exporting..";
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    var set = database.Set<LibraryItem>(transaction);
                    this.Count = set.Count;
                    using (var fileStream = File.Create(this.FileName))
                    {
                        using (var zipStream = new GZipStream(fileStream, CompressionMode.Compress))
                        {
                            try
                            {
                                var fileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                Serializer.Save(zipStream, set.Select(libraryItem => this.Export(fileNames, libraryItem)));
                            }
                            catch (OperationCanceledException)
                            {
                                //Cancelled.
                            }
                        }
                    }
                }
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual LibraryItem Export(HashSet<string> fileNames, LibraryItem libraryItem)
        {
            if (this.IsCancellationRequested)
            {
                throw new OperationCanceledException();
            }
            Logger.Write(this, LogLevel.Debug, "Exporting: {0}", libraryItem.FileName);
            this.Name = Path.GetFileName(libraryItem.FileName);
            foreach (var metaDataItem in libraryItem.MetaDatas)
            {
                if (metaDataItem.IsFile)
                {
                    metaDataItem.Value = this.Export(fileNames, metaDataItem.Value);
                }
            }
            this.Position++;
            return libraryItem;
        }

        protected virtual string Export(HashSet<string> fileNames, string fileName)
        {
            if (fileNames.Add(fileName))
            {
                Logger.Write(this, LogLevel.Debug, "Exporting: {0}", fileName);
                try
                {
                    return string.Concat("DATA{", fileName, "}=", Convert.ToBase64String(File.ReadAllBytes(fileName)));
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to export \"{0}\": {1}", fileName, e.Message);
                }
            }
            Logger.Write(this, LogLevel.Debug, "Already exported: {0}", fileName);
            return fileName;
        }

        public class Serializer
        {
            public static void Save(Stream stream, IEnumerable<LibraryItem> libraryItems)
            {
                using (var writer = XmlWriter.Create(stream, new XmlWriterSettings()
                {
                    Encoding = new UTF8Encoding(false),
                    Indent = false,
                }))
                {
                    writer.WriteStartDocument();
                    writer.WriteStartElement(Publication.Product);
                    foreach (var libraryItem in libraryItems)
                    {
                        writer.WriteStartElement(nameof(LibraryItem));
                        writer.WriteAttributeString(nameof(LibraryItem.DirectoryName), libraryItem.DirectoryName);
                        writer.WriteAttributeString(nameof(LibraryItem.FileName), libraryItem.FileName);
                        writer.WriteAttributeString(nameof(LibraryItem.ImportDate), libraryItem.ImportDate);
                        writer.WriteAttributeString(nameof(LibraryItem.Status), Convert.ToString(libraryItem.Status));
                        writer.WriteAttributeString(nameof(LibraryItem.Flags), Convert.ToString(libraryItem.Flags));
                        foreach (var metaDataItem in libraryItem.MetaDatas)
                        {
                            Save(writer, metaDataItem);
                        }
                        writer.WriteEndElement();
                    }
                    writer.WriteEndElement();
                    writer.WriteEndDocument();
                }
            }

            private static void Save(XmlWriter writer, MetaDataItem metaDataItem)
            {
                writer.WriteStartElement(nameof(MetaDataItem));
                writer.WriteAttributeString(nameof(MetaDataItem.Name), metaDataItem.Name);
                writer.WriteAttributeString(nameof(MetaDataItem.Type), Convert.ToString(metaDataItem.Type));
                writer.WriteCData(metaDataItem.Value);
                writer.WriteEndElement();
            }
        }
    }
}
