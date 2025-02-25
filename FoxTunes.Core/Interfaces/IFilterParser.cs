using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IFilterParser : IStandardComponent
    {
        bool TryParse(string filter, out IFilterParserResult result);

        bool AppliesTo(string filter, IEnumerable<string> names);
    }

    public interface IFilterParserResult : IEquatable<IFilterParserResult>
    {
        IEnumerable<IFilterParserResultGroup> Groups { get; }
    }

    public interface IFilterParserResultGroup : IEquatable<IFilterParserResultGroup>
    {
        IEnumerable<IFilterParserResultEntry> Entries { get; }
    }

    public interface IFilterParserResultEntry : IEquatable<IFilterParserResultEntry>
    {
        string Name { get; }

        FilterParserEntryOperator Operator { get; }

        string Value { get; }
    }

    public enum FilterParserEntryOperator : byte
    {
        None,
        Equal,
        Greater,
        GreaterEqual,
        Less,
        LessEqual,
        Match
    }
}
