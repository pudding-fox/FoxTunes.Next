using FoxTunes.Interfaces;
using ManagedBass.Cd;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public class BassCdMetaDataSourceCddaStrategy : BassCdMetaDataSourceStrategy
    {
        public static readonly object SyncRoot = new object();

        private static IDictionary<string, string> NAMES = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "TTITLE", CommonMetaData.Title },
            { "DARTIST", CommonMetaData.Artist },
            { "DALBUM", CommonMetaData.Album },
            { "DGENRE", CommonMetaData.Genre },
            { "DYEAR", CommonMetaData.Year }
        };

        public BassCdMetaDataSourceCddaStrategy(int drive, IEnumerable<string> hosts)
            : base(drive)
        {
            this.Hosts = hosts;
        }

        public IEnumerable<string> Hosts { get; private set; }

        public CddbTextParser Parser { get; private set; }

        public override bool Fetch()
        {
            Logger.Write(this, LogLevel.Debug, "Querying CDDB for drive {0}...", this.Drive);
            foreach (var host in this.Hosts)
            {
                try
                {
                    lock (SyncRoot)
                    {
                        Logger.Write(this, LogLevel.Debug, "Using CDDB host: {0}", host);
                        if (!string.Equals(BassCd.CDDBServer, host, StringComparison.OrdinalIgnoreCase))
                        {
                            BassCd.CDDBServer = host;
                        }
                        var id = BassCd.GetID(this.Drive, CDID.CDDB);
                        Logger.Write(this, LogLevel.Debug, "CDDB identifier is \"{0}\" for drive {1}.", id, this.Drive);
                        var sequence = BassCd.GetID(this.Drive, CDID.Read);
                        this.Parser = new CddbTextParser(id, sequence);
                        if (this.Parser.Count > 0)
                        {
                            Logger.Write(this, LogLevel.Debug, "CDDB returned {0} records for drive {1}.", this.Parser.Count, this.Drive);
                            return true;
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to query CDDB for drive {0}: {1}", this.Drive, e.Message);
                }
            }
            Logger.Write(this, LogLevel.Debug, "CDDB was empty for drive {0}.", this.Drive);
            return base.Fetch();
        }

        public override IEnumerable<MetaDataItem> GetMetaDatas(int track)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var index in new[] { -1, track })
            {
                if (this.Parser != null && this.Parser.Contains(index))
                {
                    foreach (var element in this.Parser.Get(index))
                    {
                        var name = GetName(element.Key);
                        names.Add(name);
                        yield return new MetaDataItem(name, MetaDataItemType.Tag)
                        {
                            Value = element.Value
                        };
                    }
                }
            }
            foreach (var metaDataItem in base.GetMetaDatas(track))
            {
                if (names.Contains(metaDataItem.Name))
                {
                    continue;
                }
                yield return metaDataItem;
            }
        }

        private static string GetName(string name)
        {
            if (NAMES.ContainsKey(name))
            {
                return NAMES[name];
            }
            return name;
        }
    }
}
