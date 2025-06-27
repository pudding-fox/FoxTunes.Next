using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Timers;

namespace FoxTunes
{
    public class PlaylistPopulator : PopulatorBase
    {
        public PlaylistPopulator(IDatabaseComponent database, IPlaybackManager playbackManager, int sequence, int offset, bool reportProgress, ITransactionSource transaction)
            : base(reportProgress)
        {
            this.Database = database;
            this.PlaybackManager = playbackManager;
            this.Sequence = sequence;
            this.Offset = offset;
            this.Transaction = transaction;
        }

        public IDatabaseComponent Database { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public int Sequence { get; private set; }

        public int Offset { get; private set; }

        public ITransactionSource Transaction { get; private set; }

        public string Current { get; private set; }

        public async Task Populate(Playlist playlist, IEnumerable<string> paths, CancellationToken cancellationToken)
        {
            if (this.ReportProgress)
            {
                this.Timer.Interval = FAST_INTERVAL;
                this.Timer.Start();
            }

            using (var writer = new PlaylistWriter(this.Database, this.Transaction))
            {
                foreach (var path in paths.OrderBy())
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                    if (Directory.Exists(path))
                    {
                        var fileNames = FileSystemHelper.EnumerateFiles(
                            path,
                            "*.*",
                            FileSystemHelper.SearchOption.Recursive | FileSystemHelper.SearchOption.Sort
                        );
                        foreach (var fileName in fileNames)
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }
                            Logger.Write(this, LogLevel.Debug, "Adding file to playlist: {0}", fileName);
                            var success = await this.AddPlaylistItem(playlist, writer, fileName).ConfigureAwait(false);
                            if (success && this.ReportProgress)
                            {
                                this.Current = fileName;
                            }
                        }
                    }
                    else if (File.Exists(path))
                    {
                        Logger.Write(this, LogLevel.Debug, "Adding file to playlist: {0}", path);
                        var success = await this.AddPlaylistItem(playlist, writer, path).ConfigureAwait(false);
                        if (success && this.ReportProgress)
                        {
                            this.Current = path;
                        }
                    }
                }
            }
        }

        protected virtual async Task<bool> AddPlaylistItem(Playlist playlist, PlaylistWriter writer, string fileName)
        {
            try
            {
                if (!this.PlaybackManager.IsSupported(fileName))
                {
                    Logger.Write(this, LogLevel.Debug, "File is not supported: {0}", fileName);
                    return false;
                }
                Logger.Write(this, LogLevel.Trace, "Adding file to playlist: {0}", fileName);
                var playlistItem = new PlaylistItem()
                {
                    Playlist_Id = playlist.Id,
                    DirectoryName = Path.GetDirectoryName(fileName),
                    FileName = fileName,
                    Sequence = this.Sequence,
                    Status = PlaylistItemStatus.Import
                };
                await writer.Write(playlistItem).ConfigureAwait(false);
                this.Offset++;
                return true;
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Debug, "Failed to add file \"{0}\" to playlist: {1}", fileName, e.Message);
                return false;
            }
        }

        protected override void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                if (this.Current != null)
                {
                    this.Description = Path.GetFileName(this.Current);
                }
                base.OnElapsed(sender, e);
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }
    }
}
