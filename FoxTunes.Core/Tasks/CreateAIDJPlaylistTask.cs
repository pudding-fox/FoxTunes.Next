using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace FoxTunes
{
    public class CreateAIDJPlaylistTask : PlaylistTaskBase
    {
        public CreateAIDJPlaylistTask(Playlist playlist, int history, int limit) : base(playlist, 0)
        {
            this.History = history;
            this.Limit = limit;
        }

        public int History { get; private set; }

        public int Limit { get; private set; }

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

        public IAIRuntime Runtime { get; private set; }

        public TextConfigurationElement FileId { get; private set; }

        public TextConfigurationElement VectorStoreId { get; private set; }

        public TextConfigurationElement PromptTemplate { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.Runtime = core.Components.AIRuntime;
            this.FileId = this.Configuration.GetElement<TextConfigurationElement>(
                AIBehaviourConfiguration.SECTION,
                AIBehaviourConfiguration.FILE_ID
            );
            this.VectorStoreId = this.Configuration.GetElement<TextConfigurationElement>(
                AIBehaviourConfiguration.SECTION,
                AIBehaviourConfiguration.VECTOR_STORE_ID
            );
            this.PromptTemplate = this.Configuration.GetElement<TextConfigurationElement>(
                AIBehaviourConfiguration.SECTION,
                AIBehaviourConfiguration.DJ_PROMPT_TEMPLATE
            );
        }

        protected override async Task OnRun()
        {
            if (string.IsNullOrEmpty(this.FileId.Value) && string.IsNullOrEmpty(this.VectorStoreId.Value))
            {
                throw new InvalidOperationException("Library is not available, please check your settings.");
            }
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
            this.Description = "Thinking";
            if (this.Runtime == null)
            {
                throw new InvalidOperationException("This feature requires an AI provider plugin.");
            }
            Logger.Write(this, LogLevel.Debug, "Cleating AI context.");
            using (var context = this.Runtime.CreateContext())
            {
                var store = context.CreateResponseStore();
                var attempt = 0;
                var history = await this.GetListeningHistory().ConfigureAwait(false);
                var prompt = string.Format(this.PromptTemplate.Value, history, this.Limit);
            retry:
                Logger.Write(this, LogLevel.Debug, "Sending request to AI: {0}", prompt);
                var result = default(string);
                try
                {
                    result = await store.Create(prompt, this.FileId.Value, this.VectorStoreId.Value, this.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to get response from AI: {0}", e.Message);
                    throw;
                }
                Logger.Write(this, LogLevel.Debug, "Response from AI: {0}", result);
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
                        if (this.IsCancellationRequested)
                        {
                            return;
                        }
                        goto retry;
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Debug, "Timed out.");
                        throw new TimeoutException("Timed out waiting for response.");
                    }
                }
                await this.AddPaths(paths, true).ConfigureAwait(false);
            }
        }

        private async Task<string> GetListeningHistory()
        {
            var builder = new StringBuilder();
            builder.AppendLine("\"FileName\",\"Artist\",\"Album\",\"Title\",\"LastPlayed\",\"PlayCount\"");
            using (var transaction = this.Database.BeginTransaction(this.Database.PreferredIsolationLevel))
            {
                using (var reader = this.GetListeningHistory(transaction))
                {
                    using (var sequence = reader.GetAsyncEnumerator())
                    {
                        while (await sequence.MoveNextAsync().ConfigureAwait(false))
                        {
                            var row = string.Concat(
                                "\"",
                                this.Escape(sequence.Current.Get<string>("FileName")),
                                "\",\"",
                                this.Escape(sequence.Current.Get<string>("Album")),
                                "\",\"",
                                this.Escape(sequence.Current.Get<string>("Title")),
                                "\",\"",
                                this.Escape(sequence.Current.Get<string>("LastPlayed")),
                                "\",\"",
                                this.Escape(sequence.Current.Get<string>("PlayCount")),
                                "\""
                            );
                            builder.AppendLine(row);
                        }
                    }
                }
            }
            return builder.ToString();
        }

        protected virtual IDatabaseReader GetListeningHistory(ITransactionSource transaction)
        {
            return this.Database.ExecuteReader(this.Database.Queries.GetListeningHistory, (parameters, phase) =>
            {
                switch (phase)
                {
                    case DatabaseParameterPhase.Fetch:
                        parameters["artist"] = CommonMetaData.Artist;
                        parameters["album"] = CommonMetaData.Album;
                        parameters["title"] = CommonMetaData.Title;
                        parameters["lastPlayed"] = CommonStatistics.LastPlayed;
                        parameters["playCount"] = CommonStatistics.PlayCount;
                        parameters["limit"] = this.History;
                        break;
                }
            }, transaction);
        }

        protected virtual string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }
            return value.Replace("\"", "\"\"");
        }

        protected virtual async Task<IEnumerable<string>> GetPathsFromResponse(string response)
        {
            Logger.Write(this, LogLevel.Debug, "Extracting tracks from response.");
            var paths = new List<string>();
            var regex = new Regex(@"[a-z]:[\\\/](?:[a-z0-9]+[\\\/])*[a-z0-9]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            using (var reader = new StringReader(response))
            {
                var line = default(string);
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (regex.IsMatch(line))
                    {
                        var fileName = line.Trim(' ', '"');
                        Logger.Write(this, LogLevel.Debug, "Got file name from response: {0}", fileName);
                        paths.Add(fileName);
                    }
                }
            }
            return paths;
        }
    }
}