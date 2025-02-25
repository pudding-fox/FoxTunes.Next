using FoxTunes.Interfaces;
using ManagedBass.Cd;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public class BassCdMetaDataSourceCdTextStrategy : BassCdMetaDataSourceStrategy
    {
        public static readonly object SyncRoot = new object();

        private static IDictionary<string, string> NAMES = new Dictionary<string, string>()
        {
            { "TITLE", CommonMetaData.Title },
            { "PERFORMER", CommonMetaData.Artist },
            { "COMPOSER", CommonMetaData.Composer },
            { "GENRE", CommonMetaData.Genre }
        };

        public BassCdMetaDataSourceCdTextStrategy(int drive)
            : base(drive)
        {

        }

        public CdTextParser Parser { get; private set; }

        public override bool Fetch()
        {
            Logger.Write(this, LogLevel.Debug, "Querying CD-TEXT for drive {0}...", this.Drive);
            lock (SyncRoot)
            {
                this.Parser = new CdTextParser(BassCd.GetIDText(this.Drive));
            }
            if (this.Parser.Count > 0)
            {
                Logger.Write(this, LogLevel.Debug, "CD-TEXT contains {0} records for drive {1}.", this.Parser.Count, this.Drive);
                return true;
            }
            Logger.Write(this, LogLevel.Debug, "CD-TEXT was empty for drive {0}.", this.Drive);
            return base.Fetch();
        }

        public override IEnumerable<MetaDataItem> GetMetaDatas(int track)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (this.Parser.Contains(track))
            {
                foreach (var element in this.Parser.Get(track))
                {
                    var name = GetName(element.Key);
                    names.Add(name);
                    yield return new MetaDataItem(name, MetaDataItemType.Tag)
                    {
                        Value = element.Value
                    };
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
