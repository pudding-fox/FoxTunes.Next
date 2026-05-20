using FoxDb;
using FoxDb.Interfaces;
using FoxTunes.Interfaces;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FoxTunes.Templates
{
    public partial class PlaylistFilterBuilder
    {
        public static readonly IEnumerable<FilterParserEntryOperator> NumericOperators = new[]
        {
            FilterParserEntryOperator.Greater,
            FilterParserEntryOperator.GreaterEqual,
            FilterParserEntryOperator.Less,
            FilterParserEntryOperator.LessEqual
        };

        public PlaylistFilterBuilder(IDatabase database, IFilterParserResult filter)
        {
            this.Database = database;
            this.Filter = filter;
        }

        public IDatabase Database { get; private set; }

        public IFilterParserResult Filter { get; private set; }

        public string RenderGroup(IFilterParserResultGroup group)
        {
            var builder = new StringBuilder();
            var first = true;
            foreach (var entry in group.Entries)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    builder.Append(" OR ");
                }
                var numeric = default(int);
                var isNumeric = NumericOperators.Contains(entry.Operator) && int.TryParse(entry.Value, out numeric);
                builder.Append("(");
                builder.Append("\"MetaDataItems\".\"Name\" = ");
                builder.Append(this.Database.QueryFactory.Dialect.String(entry.Name));
                builder.Append(" AND ");
                if (isNumeric)
                {
                    builder.Append("CAST(\"MetaDataItems\".\"Value\" AS int)");
                }
                else
                {
                    builder.Append("\"MetaDataItems\".\"Value\"");
                }
                switch (entry.Operator)
                {
                    default:
                    case FilterParserEntryOperator.Equal:
                        builder.Append(" = ");
                        break;
                    case FilterParserEntryOperator.Greater:
                        builder.Append(" > ");
                        break;
                    case FilterParserEntryOperator.GreaterEqual:
                        builder.Append(" >= ");
                        break;
                    case FilterParserEntryOperator.Less:
                        builder.Append(" < ");
                        break;
                    case FilterParserEntryOperator.LessEqual:
                        builder.Append(" <= ");
                        break;
                    case FilterParserEntryOperator.Match:
                        builder.Append(" LIKE ");
                        break;
                }
                if (isNumeric)
                {
                    builder.Append(numeric);
                }
                else
                {
                    var value = entry.Value.Replace(FilterParserResultEntry.BOUNDED_WILDCARD, "_").Replace(FilterParserResultEntry.UNBOUNDED_WILDCARD, "%");
                    builder.Append(this.Database.QueryFactory.Dialect.String(value));
                }
                builder.Append(")");
            }
            return builder.ToString();
        }
    }
}
