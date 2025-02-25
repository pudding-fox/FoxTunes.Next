using FoxDb.Interfaces;
using System.Collections.Generic;

namespace FoxTunes.Templates
{
    public partial class PivotViewBuilder
    {
        public PivotViewBuilder(IDatabase database, string table, IEnumerable<string> keyColumns, IEnumerable<string> nameColumns, IEnumerable<string> valueColumns, IEnumerable<string> values)
        {
            this.Database = database;
            this.Table = table;
            this.KeyColumns = keyColumns;
            this.NameColumns = nameColumns;
            this.ValueColumns = valueColumns;
            this.Values = values;
        }

        public IDatabase Database { get; private set; }

        public string Table { get; private set; }

        public IEnumerable<string> KeyColumns { get; private set; }

        public IEnumerable<string> NameColumns { get; private set; }

        public IEnumerable<string> ValueColumns { get; private set; }

        public IEnumerable<string> Values { get; private set; }
    }
}
