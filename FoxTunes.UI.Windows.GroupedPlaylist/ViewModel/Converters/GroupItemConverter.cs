using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class GroupItemConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is CollectionViewGroup collectionViewGroup)
            {
                var parent = new PlaylistItem();
                var metaDatas = new HashSet<MetaDataItem>(MetaDataItemComparer.Instance);
                foreach (var child in collectionViewGroup.Items.OfType<IFileData>())
                {
                    parent.FileName = child.FileName;
                    parent.DirectoryName = child.DirectoryName;
                    lock (child.MetaDatas)
                    {
                        foreach (var metaDataItem in child.MetaDatas)
                        {
                            if (metaDataItem.Type == MetaDataItemType.Image)
                            {
                                metaDatas.Add(metaDataItem);
                            }
                        }
                    }
                }
                if (metaDatas.Count == 0)
                {
                    //Show placeholder.
                    metaDatas.Add(new MetaDataItem());
                }
                parent.MetaDatas = metaDatas.ToList();
                return parent;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public class MetaDataItemComparer : IEqualityComparer<MetaDataItem>
        {
            public bool Equals(MetaDataItem x, MetaDataItem y)
            {
                if (object.ReferenceEquals(x, y))
                {
                    return true;
                }
                if (x.Id == y.Id)
                {
                    return true;
                }
                if (string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Value, y.Value, StringComparison.OrdinalIgnoreCase) && x.Type.Equals(y.Type))
                {
                    return true;
                }
                return false;
            }

            public int GetHashCode(MetaDataItem obj)
            {
                var hashCode = 0;
                unchecked
                {
                    if (!string.IsNullOrEmpty(obj.Name))
                    {
                        hashCode += obj.Name.ToLower().GetHashCode();
                    }
                    if (!string.IsNullOrEmpty(obj.Value))
                    {
                        hashCode += obj.Value.ToLower().GetHashCode();
                    }
                }
                return hashCode;
            }

            public static readonly IEqualityComparer<MetaDataItem> Instance = new MetaDataItemComparer();
        }
    }
}
