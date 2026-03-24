using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class CreateAIPlaylistTask : PlaylistTaskBase
    {
        public CreateAIPlaylistTask(Playlist playlist, string prompt) : base(playlist, 0)
        {
            this.Prompt = prompt;
        }

        public string Prompt { get; private set; }

        public override bool Visible
        {
            get
            {
                return true;
            }
        }

        public IAIRuntime Runtime { get; private set; }

        public TextConfigurationElement VectorStoreId { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.Runtime = core.Components.AIRuntime;
            this.VectorStoreId = this.Configuration.GetElement<TextConfigurationElement>(
                AIBehaviourConfiguration.SECTION,
                AIBehaviourConfiguration.VECTOR_STORE_ID
            );
            if (string.IsNullOrEmpty(this.VectorStoreId.Value))
            {
                throw new InvalidOperationException("Vector store has not been configured, please check your settings.");
            }
        }

        protected override async Task OnRun()
        {
            await this.RemoveItems(PlaylistItemStatus.None).ConfigureAwait(false);
            await this.AddPlaylistItems().ConfigureAwait(false);
            await this.SetPlaylistItemsStatus(PlaylistItemStatus.None).ConfigureAwait(false);
        }

        protected override async Task OnCompleted()
        {
            await base.OnCompleted().ConfigureAwait(false);
            await this.SignalEmitter.Send(new Signal(this, CommonSignals.PlaylistUpdated, new PlaylistUpdatedSignalState(this.Playlist, DataSignalType.Updated))).ConfigureAwait(false);
        }

        private async Task AddPlaylistItems()
        {
            this.Name = "Creating playlist";
            using (var task = new SingletonReentrantTask(this, ComponentSlots.Database, SingletonReentrantTask.PRIORITY_HIGH, async cancellationToken =>
            {
                using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
                {
                    await this.AddPlaylistItems(transaction).ConfigureAwait(false);
                    if (transaction.HasTransaction)
                    {
                        transaction.Commit();
                    }
                }
            }))
            {
                await task.Run().ConfigureAwait(false);
            }
        }

        private async Task AddPlaylistItems(ITransactionSource transaction)
        {
            this.Description = "Thinking";
            Logger.Write(this, LogLevel.Debug, "Cleating AI context.");
            using (var context = this.Runtime.CreateContext())
            {
                var store = context.CreateResponseStore();
                var attempt = 0;
                var prompt = string.Format("Create a playlist from my library using the prompt: {0}. Ensure that the output is in valid CSV format containing only the file name without headers.", this.Prompt);
            retry:
                Logger.Write(this, LogLevel.Debug, "Sending request to AI: {0}", prompt);
                var result = await store.Create(prompt, this.VectorStoreId.Value);
                var paths = Enumerable.Empty<string>();
                try
                {
                    paths = await this.GetPathsFromResponse(result).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to extract tracks from the response: {0}", e.Message);
                }
                if (!paths.Any())
                {
                    if (attempt++ < 5)
                    {
                        Logger.Write(this, LogLevel.Debug, "Will retry.");
                        await Task.Delay(1000).ConfigureAwait(false);
                        goto retry;
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Debug, "Timed out.");
                        throw new TimeoutException("Timed out waiting for response.");
                    }
                }
                await this.AddPlaylistItems(paths, CancellationToken.None).ConfigureAwait(false);
            }
        }

        protected virtual async Task<IEnumerable<string>> GetPathsFromResponse(string response)
        {
            using (var reader = new StringReader(response))
            {
                var line = default(string);
                var foundHeader = default(bool);
                var foundFooter = default(bool);
                Logger.Write(this, LogLevel.Debug, "Locating the csv header.");
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (line.StartsWith("```"))
                    {
                        Logger.Write(this, LogLevel.Debug, "Found the csv header.");
                        foundHeader = true;
                        break;
                    }
                }
                if (!foundHeader)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to locate the csv header.");
                    throw new InvalidOperationException("Data could not be located in the response.");
                }
                var paths = new List<string>();
                Logger.Write(this, LogLevel.Debug, "Locating the csv footer.");
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    if (line.StartsWith("```"))
                    {
                        Logger.Write(this, LogLevel.Debug, "Found the csv footer.");
                        foundFooter = true;
                        break;
                    }
                    var fileName = line.Trim(new[] { '"', ' ' });
                    try
                    {
                        if (!File.Exists(fileName))
                        {
                            Logger.Write(this, LogLevel.Warn, "File \"{0}\" does not exist, skipping.", fileName);
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to determine whether file \"{0}\" exists: {1}", fileName, e.Message);
                        continue;
                    }
                    Logger.Write(this, LogLevel.Debug, "Found file: {0}", fileName);
                    paths.Add(fileName);
                }
                if (!foundFooter)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to locate the csv footer.");
                    throw new InvalidOperationException("Data could not be located in the response.");
                }
                return paths;
            }
        }
    }
}
