using FoxTunes.DB.Sort;
using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class SortParser : StandardComponent, ISortParser
    {
        public SortParser()
        {
            this.Providers = new Lazy<IList<ISortParserProvider>>(
                () => ComponentRegistry.Instance.GetComponents<ISortParserProvider>().ToList()
            );
            this.Store = new ConcurrentDictionary<string, ISortParserResult>(StringComparer.OrdinalIgnoreCase);
        }

        public Lazy<IList<ISortParserProvider>> Providers { get; private set; }

        public ConcurrentDictionary<string, ISortParserResult> Store { get; private set; }

        public bool TryParse(string sort, out ISortParserResult result)
        {
            result = this.Store.GetOrAdd(sort, () =>
            {
                if (string.IsNullOrEmpty(sort))
                {
                    return SortParserResult.Default;
                }
                var lines = sort.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .ToArray();
                var success = default(bool);
                var expressions = new List<ISortParserResultExpression>();
                foreach (var line in lines)
                {
                    foreach (var provider in this.Providers.Value)
                    {
                        var expression = default(ISortParserResultExpression);
                        if (provider.TryParse(line, out expression))
                        {
                            expressions.Add(expression);
                            success = true;
                            break;
                        }
                    }
                    if (!success)
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to parse sort: {0}", line.Trim());
                    }
                }
                if (expressions.Any())
                {
                    return new SortParserResult(expressions);
                }
                else
                {
                    return SortParserResult.Default;
                }
            });
            return result != null;
        }

        [ComponentPriority(ComponentPriorityAttribute.LOW)]
        [ComponentDependency(Slot = ComponentSlots.Database)]
        public class DefaultSortParserProvider : SortParserProvider
        {
            public override bool TryParse(string sort, out ISortParserResultExpression expression)
            {
                if (string.IsNullOrEmpty(sort))
                {
                    expression = default(ISortParserResultExpression);
                    return false;
                }
                var name = default(string);
                if (!this.TryGetName(sort, out name))
                {
                    expression = default(ISortParserResultExpression);
                    return false;
                }
                expression = new SortParserResultExpression(name);
                return true;
            }
        }
    }

    [ComponentPriority(ComponentPriorityAttribute.NORMAL)]
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class NumericSortParserProvider : SortParserProvider
    {
        public override bool TryParse(string sort, out ISortParserResultExpression expression)
        {
            if (string.IsNullOrEmpty(sort))
            {
                expression = default(ISortParserResultExpression);
                return false;
            }
            var parts = sort.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .ToArray();
            if (parts.Length != 2)
            {
                expression = default(ISortParserResultExpression);
                return false;
            }
            var name = default(string);
            if (!this.TryGetName(parts[0], out name))
            {
                expression = default(ISortParserResultExpression);
                return false;
            }
            if (!string.Equals(parts[1], "num", StringComparison.OrdinalIgnoreCase))
            {
                expression = default(ISortParserResultExpression);
                return false;
            }
            expression = new SortParserResultExpression(name, SortParserResultOperator.Numeric);
            return true;
        }
    }

    [ComponentPriority(ComponentPriorityAttribute.HIGH)]
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class BinarySortParserProvider : SortParserProvider
    {
        private static readonly IDictionary<string, SortParserResultOperator> Operators = new Dictionary<string, SortParserResultOperator>(StringComparer.OrdinalIgnoreCase)
        {
            { "?", SortParserResultOperator.NullCoalesce }
        };

        public override bool TryParse(string sort, out ISortParserResultExpression expression)
        {
            expression = default(ISortParserResultExpression);
            if (string.IsNullOrEmpty(sort))
            {
                return false;
            }
            var parts = sort.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .ToArray();
            if (parts.Length < 3)
            {
                return false;
            }
            //TODO: I absolutely hate the way this works. If we really must create DSLs we should back them with proper ASTs instead of things like ISortParserResultExpression.
            var queue = new Queue<SortParserResultExpression>();
            for (var a = 0; a < parts.Length; a += 2)
            {
                var name = default(string);
                if (!this.TryGetName(parts[a], out name))
                {
                    return false;
                }
                var @operator = default(SortParserResultOperator);
                if (a < parts.Length - 1)
                {
                    if (!Operators.TryGetValue(parts[a + 1], out @operator))
                    {
                        return false;
                    }
                }
                queue.Enqueue(new SortParserResultExpression(name, @operator));
            }
            var parent = default(SortParserResultExpression);
            while (queue.Count > 0)
            {
                var child = queue.Dequeue();
                if (expression == null)
                {
                    expression = child;
                }
                if (parent != null)
                {
                    parent.Child = child;
                }
                parent = child;
            }
            return true;
        }
    }

    [ComponentPriority(ComponentPriorityAttribute.LOW)]
    [ComponentDependency(Slot = ComponentSlots.Database)]
    public class RandomSortParserProvider : SortParserProvider
    {
        public override bool TryParse(string sort, out ISortParserResultExpression expression)
        {
            var names = new[]
            {
                "random",
                Strings.RandomSortParserProvider_Random
            };
            if (names.Contains(sort, StringComparer.OrdinalIgnoreCase))
            {
                expression = new SortParserResultExpression(SortParserResultOperator.Random);
                return true;
            }
            expression = default(ISortParserResultExpression);
            return false;
        }
    }

    public class SortParserResult : ISortParserResult
    {
        public SortParserResult(IEnumerable<ISortParserResultExpression> expressions)
        {
            this.Expressions = expressions;
        }

        public IEnumerable<ISortParserResultExpression> Expressions { get; private set; }

        public bool IsRandom
        {
            get
            {
                return this.Expressions.All(expression => expression.Operator == SortParserResultOperator.Random);
            }
        }

        public virtual bool Equals(ISortParserResult other)
        {
            if (other == null)
            {
                return false;
            }
            if (object.ReferenceEquals(this, other))
            {
                return true;
            }
            if (!Enumerable.SequenceEqual(this.Expressions, other.Expressions))
            {
                return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ISortParserResult);
        }

        public override int GetHashCode()
        {
            var hashCode = default(int);
            unchecked
            {
                foreach (var expression in this.Expressions)
                {
                    hashCode += expression.GetHashCode();
                }
            }
            return hashCode;
        }

        public static bool operator ==(SortParserResult a, SortParserResult b)
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

        public static bool operator !=(SortParserResult a, SortParserResult b)
        {
            return !(a == b);
        }

        public static ISortParserResult Default
        {
            get
            {
                return new SortParserResult(new[]
                {
                    SortParserResultExpression.Default
                });
            }
        }
    }

    public class SortParserResultExpression : ISortParserResultExpression
    {
        public SortParserResultExpression(string name)
        {
            this.Name = name;
        }

        public SortParserResultExpression(SortParserResultOperator @operator)
        {
            this.Operator = @operator;
        }

        public SortParserResultExpression(string name, SortParserResultOperator @operator) : this(name)
        {
            this.Operator = @operator;
        }

        public SortParserResultExpression(string name, SortParserResultOperator @operator, ISortParserResultExpression child) : this(name, @operator)
        {
            this.Child = child;
        }

        public string Name { get; private set; }

        public SortParserResultOperator Operator { get; private set; }

        public ISortParserResultExpression Child { get; internal set; }

        public virtual bool Equals(ISortParserResultExpression other)
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
            if (!object.Equals(this.Child, other.Child))
            {
                return false;
            }
            return true;
        }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as ISortParserResultExpression);
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
                if (this.Child != null)
                {
                    hashCode += this.Child.GetHashCode();
                }
            }
            return hashCode;
        }

        public static bool operator ==(SortParserResultExpression a, SortParserResultExpression b)
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

        public static bool operator !=(SortParserResultExpression a, SortParserResultExpression b)
        {
            return !(a == b);
        }

        public static ISortParserResultExpression Default
        {
            get
            {
                return new SortParserResultExpression(
                    string.Format("{0}.{1}", nameof(FileSystemProperties), nameof(FileSystemProperties.FileName))
                );
            }
        }
    }
}
