using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace FoxTunes
{
    public class MetaDataItem : PersistableComponent, INamedValue
    {
        public MetaDataItem()
        {

        }

        public MetaDataItem(string name, MetaDataItemType type)
        {
            this.Name = name;
            this.Type = type;
        }

        public string Name { get; set; }

        public MetaDataItemType Type { get; set; }

        private string _Value { get; set; }

        public string Value
        {
            get
            {
                return this._Value;
            }
            set
            {
                this._Value = value;
                this.OnValueChanged();
            }
        }

        protected virtual void OnValueChanged()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Value");
        }

        public event EventHandler ValueChanged;

        public bool IsNumeric
        {
            get
            {
                if (string.IsNullOrEmpty(this.Value))
                {
                    return false;
                }
                for (var position = 0; position < this.Value.Length; position++)
                {
                    if (this.Value[position] >= '0' && this.Value[position] <= '9')
                    {
                        continue;
                    }
                    if (position == 0 && this.Value[position] == '-')
                    {
                        continue;
                    }
                    if (position > 0 && position < this.Value.Length - 1 && this.Value[position] == '.')
                    {
                        continue;
                    }
                    return false;
                }
                return true;
            }
        }

        public bool IsFile
        {
            get
            {
                if (string.IsNullOrEmpty(this.Value))
                {
                    return false;
                }
                var regex = new Regex(@"((?:[a-zA-Z]\:(\\|\/)|file\:\/\/|\\\\|\.(\/|\\))([^\\\/\:\*\?\<\>\""\|]+(\\|\/){0,1})+)");
                var matches = regex.Matches(this.Value);
                for (var a = 0; a < matches.Count; a++)
                {
                    var match = matches[a];
                    if (match.Success)
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        public override int GetHashCode()
        {
            //We need a hash code for this type for performance reasons.
            //base.GetHashCode() returns 0.
            return this.Id.GetHashCode() * 29;
        }

        public override bool Equals(IPersistableComponent other)
        {
            if (other is MetaDataItem metaDataItem)
            {
                return this.Equals(metaDataItem);
            }
            return base.Equals(other);
        }

        public virtual bool Equals(MetaDataItem other)
        {
            if (!string.Equals(this.Name, other.Name, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (this.Type != other.Type)
            {
                return false;
            }
            if (!string.Equals(this.Value, other.Value, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public static IEnumerable<string> Update(IFileData source, IFileData destination, IEnumerable<string> names)
        {
            return Update(source.MetaDatas, new[] { destination.MetaDatas }, names);
        }

        public static IEnumerable<string> Update(IFileData source, IEnumerable<IFileData> destinations, IEnumerable<string> names)
        {
            return Update(source.MetaDatas, destinations.Select(destination => destination.MetaDatas), names);
        }

        public static IEnumerable<string> Update(IEnumerable<MetaDataItem> source, ICollection<MetaDataItem> destination, IEnumerable<string> names)
        {
            return Update(source, new[] { destination }, names);
        }

        public static IEnumerable<string> Update(IEnumerable<MetaDataItem> source, IEnumerable<ICollection<MetaDataItem>> destinations, IEnumerable<string> names)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            lock (source)
            {
                var sourceMetaData = source.ToDictionary();
                foreach (var destination in destinations)
                {
                    lock (destination)
                    {
                        if (object.ReferenceEquals(source, destination))
                        {
                            continue;
                        }
                        var destinationMetaData = destination.ToDictionary();
                        foreach (var sourceMetaDataItem in source)
                        {
                            if (names != null && names.Any() && !names.Contains(sourceMetaDataItem.Name, StringComparer.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            var destinationMetaDataItem = default(MetaDataItem);
                            if (destinationMetaData.TryGetValue(sourceMetaDataItem.Name, out destinationMetaDataItem))
                            {
                                if (!string.Equals(destinationMetaDataItem.Value, sourceMetaDataItem.Value))
                                {
                                    destinationMetaDataItem.Value = sourceMetaDataItem.Value;
                                    result.Add(destinationMetaDataItem.Name);
                                }
                            }
                            else
                            {
                                destination.Add(sourceMetaDataItem);
                                result.Add(sourceMetaDataItem.Name);
                            }
                        }
                        foreach (var pair in destinationMetaData)
                        {
                            if (names != null && names.Any() && !names.Contains(pair.Value.Name, StringComparer.OrdinalIgnoreCase))
                            {
                                continue;
                            }
                            if (!sourceMetaData.ContainsKey(pair.Key))
                            {
                                pair.Value.Value = string.Empty;
                                result.Add(pair.Value.Name);
                            }
                        }
                    }
                }
            }
            return result;
        }

        public static IEnumerable<MetaDataItem> Clone(IEnumerable<MetaDataItem> metaDatas)
        {
            return metaDatas.Select(Clone);
        }

        public static MetaDataItem Clone(MetaDataItem metaDataItem)
        {
            return new MetaDataItem()
            {
                Name = metaDataItem.Name,
                Type = metaDataItem.Type,
                Value = metaDataItem.Value
            };
        }

        public static readonly MetaDataItem Empty = new MetaDataItem();
    }

    public enum MetaDataItemType : byte
    {
        None = 0,
        Tag = 1,
        Property = 2,
        Image = 4,
        Statistic = 8,
        Document = 16,
        CustomTag = 32,
        All = Tag | Property | Image | Statistic | Document | CustomTag
    }

    public enum MetaDataUpdateType : byte
    {
        None,
        System,
        User
    }

    [Flags]
    public enum MetaDataUpdateFlags : byte
    {
        None = 0,
        WriteToFiles = 1,
        ShowReport = 2,
        RefreshHierarchies = 4,
        All = WriteToFiles | ShowReport | RefreshHierarchies
    }
}
