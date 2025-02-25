using FoxTunes.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class DiscogsLookupTask : DiscogsTask
    {
        protected DiscogsLookupTask(Discogs.ReleaseLookup[] releaseLookups)
        {
            this.LookupItems = releaseLookups;
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

        public Discogs.ReleaseLookup[] LookupItems { get; private set; }

        public ICore Core { get; private set; }

        public IMetaDataManager MetaDataManager { get; private set; }

        public IReportEmitter ReportEmitter { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public DoubleConfigurationElement MinConfidence { get; private set; }

        public BooleanConfigurationElement Confirm { get; private set; }

        public BooleanConfigurationElement WriteTags { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.MetaDataManager = core.Managers.MetaData;
            this.ReportEmitter = core.Components.ReportEmitter;
            this.Configuration = core.Components.Configuration;
            this.MinConfidence = this.Configuration.GetElement<DoubleConfigurationElement>(
                DiscogsBehaviourConfiguration.SECTION,
                DiscogsBehaviourConfiguration.MIN_CONFIDENCE
            );
            this.Confirm = this.Configuration.GetElement<BooleanConfigurationElement>(
                DiscogsBehaviourConfiguration.SECTION,
                DiscogsBehaviourConfiguration.CONFIRM_LOOKUP
            );
            this.WriteTags = this.Configuration.GetElement<BooleanConfigurationElement>(
                DiscogsBehaviourConfiguration.SECTION,
                DiscogsBehaviourConfiguration.WRITE_TAGS
            );
            base.InitializeComponent(core);
        }

        protected override Task OnStarted()
        {
            this.Position = 0;
            this.Count = this.LookupItems.Length;
            return base.OnStarted();
        }

        protected override async Task OnRun()
        {
            foreach (var releaseLookup in this.LookupItems)
            {
                this.Description = releaseLookup.ToString();
                if (this.IsCancellationRequested)
                {
                    releaseLookup.Status = Discogs.ReleaseLookupStatus.Cancelled;
                    continue;
                }
                if (releaseLookup.Type == Discogs.ReleaseLookupType.None)
                {
                    Logger.Write(this, LogLevel.Warn, "Cannot fetch releases, search requires at least artist/album or title tags.");
                    releaseLookup.AddError(Strings.DiscogsLookupTask_InsufficiantData);
                    releaseLookup.Status = Discogs.ReleaseLookupStatus.Failed;
                    continue;
                }
                try
                {
                    releaseLookup.Status = Discogs.ReleaseLookupStatus.Processing;
                    var success = await this.Lookup(releaseLookup).ConfigureAwait(false);
                    if (success)
                    {
                        releaseLookup.Status = Discogs.ReleaseLookupStatus.Complete;
                    }
                    else
                    {
                        releaseLookup.AddError(Strings.DiscogsLookupTask_NotFound);
                        releaseLookup.Status = Discogs.ReleaseLookupStatus.Failed;
                    }
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Error, "Failed to lookup artwork: {0}", e.Message);
                    releaseLookup.AddError(e.Message);
                    releaseLookup.Status = Discogs.ReleaseLookupStatus.Failed;
                }
                finally
                {
                    //Save the DiscogsRelease tag (either the actual release id or none).
                    await this.SaveMetaData(releaseLookup).ConfigureAwait(false);
                }
                this.Position++;
            }
        }

        protected virtual async Task<bool> Lookup(Discogs.ReleaseLookup releaseLookup)
        {
            var description = releaseLookup.ToString();
            var master = true;
        retry:
            if (master)
            {
                Logger.Write(this, LogLevel.Debug, "Fetching master releases: {0}", description);
            }
            else
            {
                Logger.Write(this, LogLevel.Warn, "No master releases: {0}, fetching others", description);
            }
            var releases = await this.Discogs.GetReleases(releaseLookup, master).ConfigureAwait(false);
            if (!releases.Any())
            {
                Logger.Write(this, LogLevel.Warn, "No releases: {0}", description);
                return false;
            }
            Logger.Write(this, LogLevel.Debug, "Selecting releases: {0}", description);
            releaseLookup.Release = await this.ConfirmRelease(releaseLookup, releases.ToArray()).ConfigureAwait(false);
            if (releaseLookup.Release != null)
            {
                Logger.Write(this, LogLevel.Debug, "Selected {0}: {1}", description, releaseLookup.Release.Url);
                var result = await this.OnLookupSuccess(releaseLookup).ConfigureAwait(false);
                if (result)
                {
                    return result;
                }
                master = false;
                goto retry;
            }
            else
            {
                Logger.Write(this, LogLevel.Warn, "No matches: {0}", description);
            }
            return false;
        }

        protected virtual async Task<Discogs.Release> ConfirmRelease(Discogs.ReleaseLookup releaseLookup, Discogs.Release[] releases)
        {
            releases = releases.ToDictionary(
                //Map results to similarity
                release => release,
                release => release.Similarity(releaseLookup)
            ).Where(
                //Where they have the required confidence.
                pair => pair.Value >= this.MinConfidence.Value
            ).OrderByDescending(
                //Order by highest confidence first.
                pair => pair.Value
            ).ThenByDescending(
                //Then by largest cover image.
                pair => pair.Key.CoverSize
            ).ThenByDescending(
                //Then by largest thumb size.
                pair => pair.Key.ThumbSize
            ).Select(
                //Select result.
                pair => pair.Key
            ).ToArray();
            if (!this.Confirm.Value || releases.Length < 2)
            {
                return releases.FirstOrDefault();
            }
            var report = new ReleaseSelectionReport(releaseLookup, releases);
            report.InitializeComponent(this.Core);
            await this.ReportEmitter.Send(report).ConfigureAwait(false);
            if (report.SelectedRelease != null)
            {
                return report.SelectedRelease;
            }
            //Operation cancelled.
            return null;
        }

        protected abstract Task<bool> OnLookupSuccess(Discogs.ReleaseLookup releaseLookup);

        protected virtual async Task SaveMetaData(Discogs.ReleaseLookup releaseLookup)
        {
            var value = default(string);
            if (releaseLookup.Release != null)
            {
                value = releaseLookup.Release.Id;
            }
            else
            {
                value = Discogs.Release.None;
            }
            foreach (var fileData in releaseLookup.FileDatas)
            {
                lock (fileData.MetaDatas)
                {
                    fileData.AddOrUpdate(CustomMetaData.DiscogsRelease, MetaDataItemType.Tag, value);
                }
                Logger.Write(this, LogLevel.Debug, "Discogs release: {0} => {1}", fileData.FileName, value);
            }
            await this.MetaDataManager.Save(
                releaseLookup.FileDatas,
                new[] { CustomMetaData.DiscogsRelease },
                MetaDataUpdateType.System,
                MetaDataUpdateFlags.None
            ).ConfigureAwait(false);
        }
    }
}
