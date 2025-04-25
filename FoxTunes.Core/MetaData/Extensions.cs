using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public static partial class Extensions
    {
        public static T GetValueOrDefault<T>(this IFileData fileData, string name, MetaDataItemType type, T value = default(T))
        {
            var text = fileData.GetValueOrDefault(name, type);
            if (string.IsNullOrEmpty(text))
            {
                return value;
            }
            return (T)Convert.ChangeType(text, typeof(T));
        }

        public static string GetValueOrDefault(this IFileData fileData, string name, MetaDataItemType type, string value = null)
        {
            foreach (var metaDataItem in fileData.MetaDatas)
            {
                if (string.Equals(metaDataItem.Name, name, StringComparison.OrdinalIgnoreCase) && metaDataItem.Type == type)
                {
                    return metaDataItem.Value;
                }
            }
            return value;
        }

        public static MetaDataItem GetOrAdd(this IFileData fileData, string name, MetaDataItemType type, string value = null)
        {
            foreach (var metaDataItem in fileData.MetaDatas)
            {
                if (string.Equals(metaDataItem.Name, name, StringComparison.OrdinalIgnoreCase) && metaDataItem.Type == type)
                {
                    return metaDataItem;
                }
            }
            {
                var metaDataItem = new MetaDataItem(name, type);
                if (value != null)
                {
                    metaDataItem.Value = value;
                }
                fileData.MetaDatas.Add(metaDataItem);
                return metaDataItem;
            }
        }

        public static bool AddOrUpdate(this IFileData fileData, Func<IDictionary<string, MetaDataItem>, IEnumerable<MetaDataItem>> factory, out ISet<string> names)
        {
            names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return fileData.AddOrUpdate(factory, names);
        }

        public static bool AddOrUpdate(this IFileData fileData, Func<IDictionary<string, MetaDataItem>, IEnumerable<MetaDataItem>> factory, ISet<string> names)
        {
            var source = fileData.MetaDatas.ToDictionary();
            var metaDataItems = factory(source);
            foreach (var metaDataItem in metaDataItems)
            {
                if (fileData.AddOrUpdate(source, metaDataItem.Name, metaDataItem.Type, metaDataItem.Value))
                {
                    names.Add(metaDataItem.Name);
                }
            }
            return names.Any();
        }

        public static bool AddOrUpdate(this IFileData fileData, IEnumerable<MetaDataItem> metaDataItems, out ISet<string> names)
        {
            names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            return fileData.AddOrUpdate(metaDataItems, names);
        }

        public static bool AddOrUpdate(this IFileData fileData, IEnumerable<MetaDataItem> metaDataItems, ISet<string> names)
        {
            var source = fileData.MetaDatas.ToDictionary();
            foreach (var metaDataItem in metaDataItems)
            {
                if (fileData.AddOrUpdate(source, metaDataItem.Name, metaDataItem.Type, metaDataItem.Value))
                {
                    names.Add(metaDataItem.Name);
                }
            }
            return names.Any();
        }

        public static bool AddOrUpdate(this IFileData fileData, IDictionary<string, MetaDataItem> metaDataItems, string name, MetaDataItemType type, string value)
        {
            var metaDataItem = default(MetaDataItem);
            if (metaDataItems.TryGetValue(name, out metaDataItem))
            {
                if (string.Equals(metaDataItem.Value, value))
                {
                    return false;
                }
                metaDataItem.Value = value;
                return true;
            }
            fileData.MetaDatas.Add(new MetaDataItem(name, type) { Value = value });
            return true;
        }

        public static bool AddOrUpdate(this IFileData fileData, string name, MetaDataItemType type, string value)
        {
            foreach (var metaDataItem in fileData.MetaDatas)
            {
                if (string.Equals(metaDataItem.Name, name, StringComparison.OrdinalIgnoreCase) && metaDataItem.Type == type)
                {
                    if (string.Equals(metaDataItem.Value, value))
                    {
                        return false;
                    }
                    metaDataItem.Value = value;
                    return true;
                }
            }
            fileData.MetaDatas.Add(new MetaDataItem(name, type) { Value = value });
            return true;
        }
    }
}
