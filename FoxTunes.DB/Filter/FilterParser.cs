using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class FilterParser : StandardComponent, IFilterParser, IConfigurableComponent
    {
        public FilterParser()
        {
            this.Providers = new Lazy<IList<IFilterParserProvider>>(
                () => ComponentRegistry.Instance.GetComponents<IFilterParserProvider>().ToList()
            );
            this.Store = new ConcurrentDictionary<string, IFilterParserResult>(StringComparer.OrdinalIgnoreCase);
        }

        public Lazy<IList<IFilterParserProvider>> Providers { get; private set; }

        public ConcurrentDictionary<string, IFilterParserResult> Store { get; private set; }

        public bool TryParse(string filter, out IFilterParserResult result)
        {
            result = this.Store.GetOrAdd(filter, () =>
            {
                var success = default(bool);
                var groups = new List<IFilterParserResultGroup>();
                while (!string.IsNullOrEmpty(filter))
                {
                    filter = filter.Trim();
                    foreach (var provider in this.Providers.Value)
                    {
                        var currentGroups = default(IEnumerable<IFilterParserResultGroup>);
                        if (provider.TryParse(ref filter, out currentGroups))
                        {
                            groups.AddRange(currentGroups);
                            success = true;
                            break;
                        }
                    }
                    if (!success)
                    {
                        break;
                    }
                }
                if (!success && !string.IsNullOrEmpty(filter))
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to parse filter: {0}", filter);
                }
                if (groups.Any())
                {
                    return new FilterParserResult(this.PostProcess(groups).ToArray());
                }
                else
                {
                    return null;
                }
            });
            return result != null;
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

        protected virtual IEnumerable<IFilterParserResultGroup> PostProcess(IEnumerable<IFilterParserResultGroup> groups)
        {
            var result = new Dictionary<string, IList<IFilterParserResultEntry>>(StringComparer.OrdinalIgnoreCase);
            foreach (var group in groups)
            {
                if (group.Entries.Count() > 1)
                {
                    yield return group;
                    continue;
                }
                foreach (var entry in group.Entries)
                {
                    result.GetOrAdd(entry.Name, () => new List<IFilterParserResultEntry>()).Add(entry);
                }
            }
            foreach (var group in result.Values.Select(entries => new FilterParserResultGroup(entries)))
            {
                yield return group;
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return FilterParserConfiguration.GetConfigurationSections();
        }

        [ComponentPriority(ComponentPriorityAttribute.LOW)]
        [ComponentDependency(Slot = ComponentSlots.Database)]
        public class DefaultFilterParserProvider : FilterParserProvider
        {
            public ILibraryHierarchyCache LibraryHierarchyCache { get; private set; }

            public IConfiguration Configuration { get; private set; }

            public IEnumerable<string> SearchNames { get; private set; }

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

            public override bool TryParse(ref string filter, out IFilterParserResultGroup group)
            {
                if (string.IsNullOrWhiteSpace(filter))
                {
                    group = default(IFilterParserResultGroup);
                    return false;
                }
                group = this.Parse(filter);
                this.OnParsed(ref filter, 0, filter.Length);
                return true;
            }

            protected virtual IFilterParserResultGroup Parse(string filter)
            {
                var pattern = string.Format(
                    "{0}{1}{0}",
                    FilterParserResultEntry.UNBOUNDED_WILDCARD,
                    filter.Trim().Replace(" ", FilterParserResultEntry.UNBOUNDED_WILDCARD)
                );
                var entries = this.SearchNames.Select(
                    name => new FilterParserResultEntry(name, FilterParserEntryOperator.Match, pattern)
                ).ToArray();
                return new FilterParserResultGroup(entries);
            }
        }

        [ComponentPriority(ComponentPriorityAttribute.NORMAL)]
        [ComponentDependency(Slot = ComponentSlots.Database)]
        public class KeyValueFilterParserProvider : FilterParserProvider
        {
            protected const string ENTRY = "ENTRY";

            protected const string NAME = "NAME";

            protected const string OPERATOR = "OPERATOR";

            protected const string VALUE = "VALUE";

            public KeyValueFilterParserProvider()
            {
                var operators = string.Join("|", new[]
                {
                    FilterParserResultEntry.GREATER_EQUAL,
                    FilterParserResultEntry.GREATER,
                    FilterParserResultEntry.LESS_EQUAL,
                    FilterParserResultEntry.LESS,
                    FilterParserResultEntry.EQUAL
                }.Select(element => "(" + Regex.Escape(element) + ")"));
                this.Regex = new Regex(
                   this.GetExpression(operators),
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
                );
            }

            public Regex Regex { get; private set; }

            protected virtual string GetExpression(string operators)
            {
                return "^(?:(?<" + ENTRY + ">(?<" + NAME + ">[a-z]+)\\s*(?<" + OPERATOR + ">" + operators + ")\\s*(?<" + VALUE + ">.+?)))+$";
            }

            public override bool TryParse(ref string filter, out IEnumerable<IFilterParserResultGroup> groups)
            {
                if (string.IsNullOrWhiteSpace(filter))
                {
                    groups = default(IEnumerable<IFilterParserResultGroup>);
                    return false;
                }
                var match = this.Regex.Match(filter);
                if (!match.Success)
                {
                    groups = default(IEnumerable<IFilterParserResultGroup>);
                    return false;
                }
                groups = this.Parse(match);
                this.OnParsed(ref filter, match.Index, match.Length);
                return true;
            }

            protected virtual IEnumerable<IFilterParserResultGroup> Parse(Match match)
            {
                for (int a = 0, b = match.Groups[ENTRY].Captures.Count; a < b; a++)
                {
                    var name = match.Groups[NAME].Captures[a].Value.Trim();
                    var @operator = FilterParserResultEntry.GetOperator(
                        match.Groups[OPERATOR].Captures[a].Value.Trim()
                    );
                    var value = match.Groups[VALUE].Captures[a].Value.Trim();
                    switch (@operator)
                    {
                        case FilterParserEntryOperator.Match:
                            value = string.Format(
                                "{0}{1}{0}",
                                FilterParserResultEntry.UNBOUNDED_WILDCARD,
                                value
                            );
                            break;
                    }
                    yield return new FilterParserResultGroup(new FilterParserResultEntry(name, @operator, value));
                }
            }
        }

        [ComponentPriority(ComponentPriorityAttribute.HIGH)]
        [ComponentDependency(Slot = ComponentSlots.Database)]
        public class RatingFilterParserProvider : FilterParserProvider
        {
            const string RATING = "RATING";

            public RatingFilterParserProvider()
            {
                this.Regex = new Regex(
                    @"(?:(?:^|\s+)(?:(?<" + RATING + @">[0-5])\\*)(?:$|\s+))",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
                );
            }

            public Regex Regex { get; private set; }

            public override bool TryParse(ref string filter, out IFilterParserResultGroup result)
            {
                if (string.IsNullOrWhiteSpace(filter))
                {
                    result = default(IFilterParserResultGroup);
                    return false;
                }
                var match = this.Regex.Match(filter);
                if (!match.Success)
                {
                    result = default(IFilterParserResultGroup);
                    return false;
                }
                result = this.Parse(match);
                this.OnParsed(ref filter, match.Index, match.Length);
                return true;
            }

            protected virtual IFilterParserResultGroup Parse(Match match)
            {
                var value = match.Groups[RATING].Value;
                return new FilterParserResultGroup(new FilterParserResultEntry(CommonStatistics.Rating, FilterParserEntryOperator.Equal, value));
            }
        }

        [ComponentPriority(ComponentPriorityAttribute.HIGH)]
        [ComponentDependency(Slot = ComponentSlots.Database)]
        public class LikeFilterParserProvider : FilterParserProvider
        {
            public LikeFilterParserProvider()
            {
                this.Regex = new Regex(
                    @"(?:(?:^|\s+)(like|love)(?:$|\s+))",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
                );
            }

            public Regex Regex { get; private set; }

            public override bool TryParse(ref string filter, out IFilterParserResultGroup result)
            {
                if (string.IsNullOrWhiteSpace(filter))
                {
                    result = default(IFilterParserResultGroup);
                    return false;
                }
                var match = this.Regex.Match(filter);
                if (!match.Success)
                {
                    result = default(IFilterParserResultGroup);
                    return false;
                }
                result = this.Parse(match);
                this.OnParsed(ref filter, match.Index, match.Length);
                return true;
            }

            protected virtual IFilterParserResultGroup Parse(Match match)
            {
                return new FilterParserResultGroup(new FilterParserResultEntry(CommonStatistics.Like, FilterParserEntryOperator.Equal, bool.TrueString));
            }
        }

        [ComponentPriority(ComponentPriorityAttribute.HIGH)]
        [ComponentDependency(Slot = ComponentSlots.Database)]
        public class HighDefFilterParserProvider : FilterParserProvider
        {
            public HighDefFilterParserProvider()
            {
                this.Regex = new Regex(
                    @"(?:(?:^|\s+)(HD|high\-def)(?:$|\s+))",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture
                );
            }

            public Regex Regex { get; private set; }

            public override bool TryParse(ref string filter, out IFilterParserResultGroup result)
            {
                if (string.IsNullOrWhiteSpace(filter))
                {
                    result = default(IFilterParserResultGroup);
                    return false;
                }
                var match = this.Regex.Match(filter);
                if (!match.Success)
                {
                    result = default(IFilterParserResultGroup);
                    return false;
                }
                result = this.Parse(match);
                this.OnParsed(ref filter, match.Index, match.Length);
                return true;
            }

            protected virtual IFilterParserResultGroup Parse(Match match)
            {
                return new FilterParserResultGroup(new[]
                {
                    new FilterParserResultEntry(CommonProperties.AudioSampleRate, FilterParserEntryOperator.Greater, Convert.ToString(44100)),
                    new FilterParserResultEntry(CommonProperties.BitsPerSample, FilterParserEntryOperator.Greater, Convert.ToString(16))
                });
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
        public FilterParserResultGroup(IFilterParserResultEntry entry)
        {
            this.Entries = new[] { entry };
        }

        public FilterParserResultGroup(IEnumerable<IFilterParserResultEntry> entries)
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
