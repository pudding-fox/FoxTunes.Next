using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FoxTunes.AI.Tasks
{
    public class CreateAILibraryTask : BackgroundTask
    {
        public const string ID = "F50322A9-06A4-43EA-9659-F7C76B1F8A5C";

        public CreateAILibraryTask(string fileId, string vectorStoreId) : base(ID)
        {
            this.FileId = fileId;
            this.VectorStoreId = vectorStoreId;
        }

        public string FileId { get; private set; }

        public string VectorStoreId { get; private set; }

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

        public IDatabaseFactory DatabaseFactory { get; private set; }

        public IAIRuntime Runtime { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.DatabaseFactory = core.Factories.Database;
            this.Runtime = core.Components.AIRuntime;
            base.InitializeComponent(core);
        }

        protected override async Task OnRun()
        {
            this.Name = "Uploading library";
            if (this.Runtime == null)
            {
                throw new InvalidOperationException("This feature requires an AI provider plugin.");
            }
            Logger.Write(this, LogLevel.Debug, "Cleating AI context.");
            using (var context = this.Runtime.CreateContext())
            {
                if (!string.IsNullOrEmpty(this.FileId))
                {
                    Logger.Write(this, LogLevel.Debug, "Cleaning up file store.");
                    this.Description = "Cleaning up file store";
                    var store = context.CreateFileStore();
                    try
                    {
                        await store.Delete(this.FileId).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to clean up file store: {0}", e.Message);
                    }
                }
                if (!string.IsNullOrEmpty(this.VectorStoreId))
                {
                    this.Description = "Cleaning up vector store";
                    var store = context.CreateVectorStore();
                    Logger.Write(this, LogLevel.Debug, "Cleaning up vector store.");
                    try
                    {
                        await store.Delete(this.VectorStoreId).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to clean up vector store: {0}", e.Message);
                    }
                }
                {
                    Logger.Write(this, LogLevel.Debug, "Fetching library.");
                    this.Description = "Fetching library";
                    using (var stream = await this.GetEntireLibrary().ConfigureAwait(false))
                    {
                        if (stream.Length <= 1024)
                        {
                            Logger.Write(this, LogLevel.Warn, "Library doesn't have enough tracks to be useful.");
                            return;
                        }
                        Logger.Write(this, LogLevel.Debug, "library.txt is {0} bytes.", stream.Length);
                        Logger.Write(this, LogLevel.Debug, "Creating file store.");
                        this.Description = "Creating file store";
                        var store = context.CreateFileStore();
                        this.FileId = await store.Create(stream, "library.txt", this.CancellationToken).ConfigureAwait(false);
                    }
                }
                {
                    Logger.Write(this, LogLevel.Debug, "Creating vectore store.");
                    this.Description = "Creating vectore store";
                    var store = context.CreateVectorStore();
                    this.VectorStoreId = await store.Create("library.txt", this.CancellationToken).ConfigureAwait(false);
                    if (string.IsNullOrEmpty(this.VectorStoreId))
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to create vector store.");
                        return;
                    }
                    await store.AddFile(this.VectorStoreId, this.FileId, this.CancellationToken).ConfigureAwait(false);
                }
            }
        }

        private async Task<Stream> GetEntireLibrary()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            using (var database = this.DatabaseFactory.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    using (var reader = this.GetEntireLibrary(database, transaction))
                    {
                        using (var sequence = reader.GetAsyncEnumerator())
                        {
                            while (await sequence.MoveNextAsync().ConfigureAwait(false))
                            {
                                var row = string.Concat(
                                    "{\"FileName\":\"",
                                    this.Safe(sequence.Current.Get<string>("FileName")),
                                    "\",\"Artist\":\"",
                                    this.Safe(sequence.Current.Get<string>("Artist")),
                                    "\",\"Album\":\"",
                                    this.Safe(sequence.Current.Get<string>("Album")),
                                    "\",\"Title\":\"",
                                    this.Safe(sequence.Current.Get<string>("Title")),
                                    "\",\"Genre\":\"",
                                    this.Safe(sequence.Current.Get<string>("Genre")),
                                    "\",\"Year\":\"",
                                    this.Safe(sequence.Current.Get<string>("Year")),
                                    "\"}"
                                );
                                writer.WriteLine(row);
                            }
                        }
                    }
                }
            }
            await writer.FlushAsync().ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        protected virtual IDatabaseReader GetEntireLibrary(IDatabaseComponent database, ITransactionSource transaction)
        {
            return database.ExecuteReader(database.Queries.GetEntireLibrary, (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        parameters["artist"] = CommonMetaData.Artist;
                        parameters["album"] = CommonMetaData.Album;
                        parameters["track"] = CommonMetaData.Track;
                        parameters["title"] = CommonMetaData.Title;
                        parameters["genre"] = CommonMetaData.Genre;
                        parameters["year"] = CommonMetaData.Year;
                        parameters["like"] = CommonStatistics.Like;
                        parameters["rating"] = CommonStatistics.Rating;
                        break;
                }
            }, transaction);
        }

        private string Safe(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "Unknown";
            }
            var builder = new StringBuilder(value.Length + 16);
            foreach (var character in value)
            {
                switch (character)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    default:
                        builder.Append(character);
                        break;
                }
            }
            return builder.ToString();
        }
    }
}