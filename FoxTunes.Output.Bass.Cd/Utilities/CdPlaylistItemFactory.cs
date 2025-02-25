using FoxTunes.Interfaces;
using ManagedBass.Cd;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FoxTunes
{
    public class CdPlaylistItemFactory : PopulatorBase
    {
        public CdPlaylistItemFactory(int drive, bool cdLookup, IEnumerable<string> cdLookupHosts, bool reportProgress) : base(reportProgress)
        {
            this.Drive = drive;
            this.CdLookup = cdLookup;
            this.CdLookupHosts = cdLookupHosts;
        }

        public int Drive { get; private set; }

        public bool CdLookup { get; private set; }

        public IEnumerable<string> CdLookupHosts { get; private set; }

        public ICore Core { get; private set; }

        public IMetaDataSource MetaDataSource { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.MetaDataSource = new BassCdMetaDataSource(this.GetStrategy());
            this.MetaDataSource.InitializeComponent(this.Core);
            base.InitializeComponent(core);
        }

        public async Task<PlaylistItem[]> Create(CancellationToken cancellationToken)
        {
            if (this.ReportProgress)
            {
                this.Name = Strings.CdPlaylistItemFactory_Name;
            }

            var info = default(CDInfo);
            BassUtils.OK(BassCd.GetInfo(this.Drive, out info));
            var id = BassCd.GetID(this.Drive, CDID.CDPlayer);
            var directoryName = string.Format("{0}:\\", info.DriveLetter);
            var playlistItems = new List<PlaylistItem>();
            for (int a = 0, b = BassCd.GetTracks(this.Drive); a < b; a++)
            {
                if (BassCd.GetTrackLength(this.Drive, a) == -1)
                {
                    //Not a music track.
                    continue;
                }
                var fileName = CdUtils.CreateUrl(this.Drive, id, a);
                if (this.ReportProgress)
                {
                    this.Description = Path.GetFileName(fileName);
                }
                var metaData = default(MetaDataItem[]);
                try
                {
                    metaData = (
                        await this.MetaDataSource.GetMetaData(fileName).ConfigureAwait(false)
                    ).ToArray();
                }
                catch (Exception e)
                {
                    metaData = new MetaDataItem[] { };
                    Logger.Write(this, LogLevel.Debug, "Failed to read meta data from file \"{0}\": {1}", fileName, e.Message);
                }
                Logger.Write(this, LogLevel.Debug, "Adding file to playlist: {0}", fileName);
                var playlistItem = new PlaylistItem()
                {
                    DirectoryName = directoryName,
                    FileName = this.GetFileName(fileName, a, metaData),
                    Status = PlaylistItemStatus.Import
                };
                playlistItem.MetaDatas = metaData;
                playlistItems.Add(playlistItem);
            }
            return playlistItems.ToArray();
        }

        protected virtual string GetFileName(string fileName, int track, IEnumerable<MetaDataItem> metaDatas)
        {
            var metaData = metaDatas.ToDictionary2();
            var title = metaData.GetValueOrDefault(CommonMetaData.Title);
            if (!string.IsNullOrEmpty(title))
            {
                var sanitize = new Func<string, string>(value =>
                {
                    const char PLACEHOLDER = '_';
                    var characters = Enumerable.Concat(
                        Path.GetInvalidPathChars(),
                        Path.GetInvalidFileNameChars()
                    );
                    foreach (var character in characters)
                    {
                        value = value.Replace(character, PLACEHOLDER);
                    }
                    return value;
                });
                return string.Format("{0}/{1:00} - {2}.cda", fileName, track + 1, sanitize(title));
            }
            else
            {
                return string.Format("{0}/Track {0}.cda", fileName, track + 1);
            }
        }

        private IBassCdMetaDataSourceStrategy GetStrategy()
        {
            if (this.CdLookup)
            {
                var strategy = new BassCdMetaDataSourceCddaStrategy(this.Drive, this.CdLookupHosts);
                if (strategy.Fetch())
                {
                    return strategy;
                }
            }
            {
                var strategy = new BassCdMetaDataSourceCdTextStrategy(this.Drive);
                if (strategy.Fetch())
                {
                    return strategy;
                }
            }
            return new BassCdMetaDataSourceStrategy(this.Drive);
        }
    }
}
