using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class FilterParser : StandardComponent, IFilterParser, IConfigurableComponent
    {
        public FilterParserResultConverter Converter { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Converter = new FilterParserResultConverter();
            this.Converter.InitializeComponent(core);
            base.InitializeComponent(core);
        }

        public bool TryParse(string filter, out IFilterParserResult result)
        {
            var node = this.Parse(filter);
            if (node == null)
            {
                result = default(IFilterParserResult);
                return false;
            }
            result = this.Converter.Convert(node);
            return true;
        }

        public bool AppliesTo(string filter, IEnumerable<string> names)
        {
            var result = default(IFilterParserResult);
            if (!this.TryParse(filter, out result))
            {
                return false;
            }
            foreach (var group in result.Groups)
            {
                foreach (var entry in group.Entries)
                {
                    if (names.Contains(entry.Name, StringComparer.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return FilterParserConfiguration.GetConfigurationSections();
        }

        public abstract class FilterNode
        {
        }

        public class AndNode : FilterNode
        {
            public AndNode(IEnumerable<FilterNode> children)
            {
                this.Children = children;
            }

            public IEnumerable<FilterNode> Children { get; private set; }
        }

        public class FieldNode : FilterNode
        {
            public FieldNode(string name, FilterParserEntryOperator @operator, string value)
            {
                this.Name = name;
                this.Operator = @operator;
                this.Value = value;
            }

            public string Name { get; private set; }

            public FilterParserEntryOperator Operator { get; private set; }

            public string Value { get; private set; }
        }

        public class KeywordNode : FilterNode
        {
            public KeywordNode(string name)
            {
                this.Name = name;
            }

            public string Name { get; private set; }
        }

        public class FlagNode : FilterNode
        {
            public FlagNode(string name)
            {
                this.Name = name;
            }

            public string Name { get; private set; }
        }

        public class CompositeNode : FilterNode
        {
            public CompositeNode(IEnumerable<FilterNode> children)
            {
                this.Children = children;
            }

            public IEnumerable<FilterNode> Children { get; private set; }
        }

        public enum TokenType
        {
            Flag,
            Rating,
            KeyValue,
            Keyword
        }

        public class Token
        {
            public Token(TokenType type, string value)
            {
                this.Type = type;
                this.Value = value;
            }

            public TokenType Type { get; private set; }

            public string Value { get; private set; }
        }

        protected virtual FilterNode Parse(string input)
        {
            var tokens = this.Tokenize(input);
            var children = new List<FilterNode>();
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                switch (token.Type)
                {
                    case TokenType.Rating:
                        children.Add(new FieldNode(CommonStatistics.Rating, FilterParserEntryOperator.Equal, token.Value));
                        break;
                    case TokenType.Flag:
                        children.Add(ParseFlag(token.Value));
                        break;
                    case TokenType.KeyValue:
                        var field = default(FieldNode);
                        if (this.TryParseKeyValue(token.Value, out field))
                        {
                            children.Add(field);
                        }
                        break;
                    case TokenType.Keyword:
                        children.Add(new KeywordNode(token.Value));
                        break;
                }
            }
            return new AndNode(children);
        }

        protected virtual List<Token> Tokenize(string input)
        {
            var tokens = new List<Token>();
            var parts = new List<string>();
            var builder = new StringBuilder();
            var quoted = false;
            foreach (var character in input)
            {
                if (character == '"')
                {
                    quoted = !quoted;
                    continue;
                }
                if (char.IsWhiteSpace(character) && !quoted)
                {
                    if (builder.Length > 0)
                    {
                        parts.Add(builder.ToString());
                        builder.Clear();
                    }
                    continue;
                }
                builder.Append(character);
            }
            if (builder.Length > 0)
            {
                parts.Add(builder.ToString());
            }
            foreach (var part in parts)
            {
                if (part.EndsWith("*"))
                {
                    var rating = default(int);
                    if (int.TryParse(part.TrimEnd('*'), out rating))
                    {
                        tokens.Add(new Token(TokenType.Rating, Convert.ToString(rating)));
                        continue;
                    }
                }
                if (part.Equals("like", StringComparison.OrdinalIgnoreCase) || part.Equals("love", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new Token(TokenType.Flag, "like"));
                    continue;
                }
                if (part.Equals("HD", StringComparison.OrdinalIgnoreCase) || part.Equals("high-def", StringComparison.OrdinalIgnoreCase))
                {
                    tokens.Add(new Token(TokenType.Flag, "HD"));
                    continue;
                }
                if (part.Contains(":") || part.Contains(">") || part.Contains("<"))
                {
                    tokens.Add(new Token(TokenType.KeyValue, part));
                    continue;
                }
                tokens.Add(new Token(TokenType.Keyword, part));
            }
            return tokens;
        }


        protected virtual FilterNode ParseFlag(string value)
        {
            if (value == "like")
            {
                return new FieldNode(CommonStatistics.Like, FilterParserEntryOperator.Equal, bool.TrueString);
            }
            if (value == "HD")
            {
                var children = new[]
                {
                    new FieldNode(CommonProperties.AudioSampleRate, FilterParserEntryOperator.Greater, "44100"),
                    new FieldNode(CommonProperties.BitsPerSample,FilterParserEntryOperator.Greater, "16")
                };
                return new CompositeNode(children);
            }

            return new FlagNode(value);
        }

        protected virtual bool TryParseKeyValue(string input, out FieldNode node)
        {
            var match = Regex.Match(input, @"(?<name>[a-zA-Z0-9]+)\s*(?<op>>:|<:|:|>|<)\s*(?<value>.+)");
            if (!match.Success)
            {
                node = default(FieldNode);
                return false;
            }
            var name = match.Groups["name"].Value;
            var op = match.Groups["op"].Value;
            var value = match.Groups["value"].Value;
            node = new FieldNode(name, this.MapOperator(op), this.Normalize(op, value));
            return true;
        }

        protected virtual FilterParserEntryOperator MapOperator(string op)
        {
            switch (op)
            {
                default:
                case ":":
                    return FilterParserEntryOperator.Match;
                case ">":
                    return FilterParserEntryOperator.Greater;
                case ">:":
                    return FilterParserEntryOperator.GreaterEqual;
                case "<":
                    return FilterParserEntryOperator.Less;
                case "<:":
                    return FilterParserEntryOperator.LessEqual;
            }
        }

        protected virtual string Normalize(string op, string value)
        {
            if (op == ":")
            {
                return "*" + value.Replace(' ', '*') + "*";
            }
            return value;
        }

        public bool Matches(FilterNode node, IEnumerable<string> names)
        {
            if (node is AndNode and)
            {
                return and.Children.All(n => Matches(n, names));
            }
            if (node is KeywordNode keyword)
            {
                return names.Contains(keyword.Name, StringComparer.OrdinalIgnoreCase);
            }
            if (node is FlagNode flag)
            {
                return names.Contains(flag.Name, StringComparer.OrdinalIgnoreCase);
            }
            if (node is FieldNode field)
            {
                return names.Contains(field.Name, StringComparer.OrdinalIgnoreCase);
            }
            if (node is CompositeNode composite)
            {
                return composite.Children.Any(n => Matches(n, names));
            }
            return false;
        }

        public class FilterParserResultConverter : BaseComponent
        {
            public ILibraryHierarchyCache LibraryHierarchyCache { get; private set; }

            public IConfiguration Configuration { get; private set; }

            public override void InitializeComponent(ICore core)
            {
                this.LibraryHierarchyCache = core.Components.LibraryHierarchyCache;
                this.Configuration = core.Components.Configuration;
                this.Configuration.GetElement<TextConfigurationElement>(
                    FilterParserConfiguration.SECTION,
                    FilterParserConfiguration.SEARCH_NAMES
                ).ConnectValue(value =>
                {
                    var reset = this.SearchNames != null;
                    this.SearchNames = FilterParserConfiguration.GetSearchNames(value);
                    if (reset)
                    {
                        //As the results of searches are now different we should clear the cache.
                        this.LibraryHierarchyCache.Reset();
                    }
                });
                base.InitializeComponent(core);
            }

            public IEnumerable<string> SearchNames { get; private set; }

            public IFilterParserResult Convert(FilterNode node)
            {
                var result = new ConcurrentDictionary<string, IFilterParserResultGroup>(StringComparer.OrdinalIgnoreCase);
                var sequence = Visit(node).ToArray();
                //Group by entry name, this turns multiple filters on the same meta data name into an OR rather than AND.
                foreach (var element in sequence)
                {
                    if (element.Entries.Count() == 1)
                    {
                        foreach (var entry in element.Entries)
                        {
                            result.AddOrUpdate(
                                entry.Name,
                                key => new FilterParserResultGroup(entry),
                                (key, group) => new FilterParserResultGroup(group.Entries.Concat(new[] { entry }))
                            );
                        }
                    }
                    else
                    {
                        result[Guid.NewGuid().ToString("d")] = element;
                    }
                }
                return new FilterParserResult(result.Values);
            }

            private IEnumerable<IFilterParserResultGroup> Visit(FilterNode node)
            {
                if (node is AndNode and)
                {
                    foreach (var child in and.Children)
                    {
                        foreach (var group in this.Visit(child))
                        {
                            yield return group;
                        }
                    }
                }
                else if (node is CompositeNode composite)
                {
                    var entries = new List<IFilterParserResultEntry>();
                    foreach (var child in composite.Children)
                    {
                        foreach (var group in this.Visit(child))
                        {
                            entries.AddRange(group.Entries);
                        }
                    }
                    yield return new FilterParserResultGroup(entries);
                }
                else if (node is FieldNode field)
                {
                    yield return new FilterParserResultGroup(
                        new FilterParserResultEntry(
                            field.Name,
                            field.Operator,
                            field.Value
                        )
                    );
                }
                else if (node is FlagNode flag)
                {
                    yield return new FilterParserResultGroup(
                        new FilterParserResultEntry(
                            flag.Name,
                            FilterParserEntryOperator.Equal,
                            bool.TrueString
                        )
                    );
                }
                else if (node is KeywordNode keyword)
                {
                    yield return new FilterParserResultGroup(
                        this.SearchNames.Select(name =>
                            new FilterParserResultEntry(
                                name,
                                FilterParserEntryOperator.Match,
                                string.Concat("*", keyword.Name, "*")
                            )
                        )
                    );
                }
            }
        }
    }

    public class FilterParserResult : IFilterParserResult
    {
        public FilterParserResult(IEnumerable<IFilterParserResultGroup> groups)
        {
            this.Groups = groups;
        }

        public IEnumerable<IFilterParserResultGroup> Groups { get; private set; }

        public virtual bool Equals(IFilterParserResult other)
        {
            if (other == null)
            {
                return false;
            }
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            if (!Enumerable.SequenceEqual(this.Groups, other.Groups))
            {
                return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as IFilterParserResult);
        }

        public override int GetHashCode()
        {
            var hashCode = default(int);
            unchecked
            {
                foreach (var group in this.Groups)
                {
                    hashCode += group.GetHashCode();
                }
            }
            return hashCode;
        }

        public static bool operator ==(FilterParserResult a, FilterParserResult b)
        {
            if ((object)a == null && (object)b == null)
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            if (object.ReferenceEquals((object)a, (object)b))
            {
                return true;
            }
            return a.Equals(b);
        }

        public static bool operator !=(FilterParserResult a, FilterParserResult b)
        {
            return !(a == b);
        }
    }

    public class FilterParserResultGroup : IFilterParserResultGroup
    {
        public FilterParserResultGroup()
        {

        }

        public FilterParserResultGroup(IFilterParserResultEntry entry) : this()
        {
            this.Entries = new[] { entry };
        }

        public FilterParserResultGroup(IEnumerable<IFilterParserResultEntry> entries) : this()
        {
            this.Entries = entries;
        }

        public IEnumerable<IFilterParserResultEntry> Entries { get; private set; }

        public virtual bool Equals(IFilterParserResultGroup other)
        {
            if (other == null)
            {
                return false;
            }
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            if (!Enumerable.SequenceEqual(this.Entries, other.Entries))
            {
                return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as IFilterParserResultGroup);
        }

        public override int GetHashCode()
        {
            var hashCode = default(int);
            unchecked
            {
                foreach (var entry in this.Entries)
                {
                    hashCode += entry.GetHashCode();
                }
            }
            return hashCode;
        }

        public static bool operator ==(FilterParserResultGroup a, FilterParserResultGroup b)
        {
            if ((object)a == null && (object)b == null)
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            if (object.ReferenceEquals((object)a, (object)b))
            {
                return true;
            }
            return a.Equals(b);
        }

        public static bool operator !=(FilterParserResultGroup a, FilterParserResultGroup b)
        {
            return !(a == b);
        }
    }

    public class FilterParserResultEntry : IFilterParserResultEntry
    {
        public const string BOUNDED_WILDCARD = "?";

        public const string UNBOUNDED_WILDCARD = "*";

        public const string EQUAL = ":";

        public const string GREATER = ">";

        public const string GREATER_EQUAL = ">:";

        public const string LESS = "<";

        public const string LESS_EQUAL = "<:";

        public static FilterParserEntryOperator GetOperator(string @operator)
        {
            switch (@operator)
            {
                default:
                case EQUAL:
                    return FilterParserEntryOperator.Match;
                case GREATER:
                    return FilterParserEntryOperator.Greater;
                case GREATER_EQUAL:
                    return FilterParserEntryOperator.GreaterEqual;
                case LESS:
                    return FilterParserEntryOperator.Less;
                case LESS_EQUAL:
                    return FilterParserEntryOperator.LessEqual;
            }
        }

        public FilterParserResultEntry(string name, FilterParserEntryOperator @operator, string value)
        {
            this.Name = name;
            this.Operator = @operator;
            this.Value = value;
        }

        public string Name { get; private set; }

        public FilterParserEntryOperator Operator { get; private set; }

        public string Value { get; private set; }

        public virtual bool Equals(IFilterParserResultEntry other)
        {
            if (other == null)
            {
                return false;
            }
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            if (!string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (this.Operator != other.Operator)
            {
                return false;
            }
            if (!string.Equals(this.Value, other.Value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as IFilterParserResultEntry);
        }

        public override int GetHashCode()
        {
            var hashCode = default(int);
            unchecked
            {
                if (!string.IsNullOrEmpty(this.Name))
                {
                    hashCode += this.Name.ToLower().GetHashCode();
                }
                hashCode += this.Operator.GetHashCode();
                if (!string.IsNullOrEmpty(this.Value))
                {
                    hashCode += this.Value.ToLower().GetHashCode();
                }
            }
            return hashCode;
        }

        public static bool operator ==(FilterParserResultEntry a, FilterParserResultEntry b)
        {
            if ((object)a == null && (object)b == null)
            {
                return true;
            }
            if ((object)a == null || (object)b == null)
            {
                return false;
            }
            if (object.ReferenceEquals((object)a, (object)b))
            {
                return true;
            }
            return a.Equals(b);
        }

        public static bool operator !=(FilterParserResultEntry a, FilterParserResultEntry b)
        {
            return !(a == b);
        }
    }
}
