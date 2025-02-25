using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface ISortParser : IStandardComponent
    {
        bool TryParse(string sort, out ISortParserResult result);
    }

    public interface ISortParserResult : IEquatable<ISortParserResult>
    {
        IEnumerable<ISortParserResultExpression> Expressions { get; }

        bool IsRandom { get; }
    }

    public interface ISortParserResultExpression : IEquatable<ISortParserResultExpression>
    {
        string Name { get; }

        SortParserResultOperator Operator { get; }

        ISortParserResultExpression Child { get; }
    }

    public enum SortParserResultOperator : byte
    {
        None,
        Numeric,
        NullCoalesce,
        Random
    }
}
