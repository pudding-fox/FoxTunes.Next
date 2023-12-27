using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FoxTunes
{
    public class LibraryPopulator : PopulatorBase
    {
        public LibraryPopulator(IDatabaseComponent database, IPlaybackManager playbackManager, bool reportProgress, ITransactionSource transaction)
            : base(reportProgress)
        {
            this.Database = database;
            this.PlaybackManager = playbackManager;
            this.Transaction = transaction;
        }

        public IDatabaseComponent Database { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public ITransactionSource Transaction { get; private set; }

        public string Current { get; private set; }

        private volatile int position = 0;

        public async Task Populate(IEnumerable<string> paths, CancellationToken cancellationToken)
        {
            if (this.ReportProgress)
            {
                this.Name = "Populating library";
                this.Position = 0;
                this.Timer.Interval = FAST_INTERVAL;
                this.Timer.Start();
            }

            using (var writer = new LibraryWriter(this.Database, this.Transaction))
            {
                foreach (var path in paths)
                {
                    if (Directory.Exists(path))
                    {
                        foreach (var fileName in FileSystemHelper.EnumerateFiles(path, "*.*", FileSystemHelper.SearchOption.Recursive))
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                return;
                            }
                            var success = await this.AddLibraryItem(writer, fileName).ConfigureAwait(false);
                            if (success && this.ReportProgress)
                            {
                                this.Current = fileName;
                                Interlocked.Increment(ref this.position);
                            }
                        }
                    }
                    else if (File.Exists(path))
                    {
                        var success = await this.AddLibraryItem(writer, path).ConfigureAwait(false);
                        if (success && this.ReportProgress)
                        {
                            this.Current = path;
                            Interlocked.Increment(ref this.position);
                        }
                    }
                }
            }
        }

        protected virtual async Task<bool> AddLibraryItem(LibraryWriter writer, string fileName)
        {
            try
            {
                if (!this.PlaybackManager.IsSupported(fileName))
                {
                    Logger.Write(this, LogLevel.Debug, "File is not supported: {0}", fileName);
                    return false;
                }
                Logger.Write(this, LogLevel.Trace, "Adding file to library: {0}", fileName);
                var libraryItem = new LibraryItem()
                {
                    DirectoryName = Path.GetDirectoryName(fileName),
                    FileName = fileName,
                    Status = LibraryItemStatus.Import
                };
                libraryItem.SetImportDate(DateTime.UtcNow);
                await writer.Write(libraryItem).ConfigureAwait(false);
                return true;
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Debug, "Failed to add file \"{0}\" to library: {0}", fileName, e.Message);
                return false;
            }
        }

        protected override void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var position = Interlocked.Exchange(ref this.position, 0);
                if (position != 0)
                {
                    //Interval is fixed at 100ms.
                    position *= 10;
                    this.Name = string.Format("Populating library: {0} items/s", this.CountMetric.Average(position));
                    if (this.Current != null)
                    {
                        this.Description = Path.GetFileName(this.Current);
                    }
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
