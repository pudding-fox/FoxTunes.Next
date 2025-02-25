using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;

namespace FoxTunes.ViewModel
{
    public class LibraryHierarchyNodeConverter : IValueConverter
    {
        public const int MAX_IMAGES = 4;

        public static readonly ILibraryHierarchyBrowser LibraryHierarchyBrowser = ComponentRegistry.Instance.GetComponent<ILibraryHierarchyBrowser>();

        public static readonly IMetaDataBrowser MetaDataBrowser = ComponentRegistry.Instance.GetComponent<IMetaDataBrowser>();

        public static readonly IOnDemandMetaDataProvider OnDemandMetaDataProvider = ComponentRegistry.Instance.GetComponent<IOnDemandMetaDataProvider>();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LibraryHierarchyNode libraryHierarchyNode)
            {
                return new AsyncResult<IFileData>(this.Convert(libraryHierarchyNode));
            }
            return value;
        }

        public async Task<IFileData> Convert(LibraryHierarchyNode libraryHierarchyNode)
        {
            var metaDataItems = await MetaDataBrowser.GetMetaDatas(
                libraryHierarchyNode,
                CommonImageTypes.FrontCover,
                MetaDataItemType.Image,
                MAX_IMAGES
            ).ConfigureAwait(false);
            if (!metaDataItems.Any())
            {
                if (OnDemandMetaDataProvider.IsSourceEnabled(CommonImageTypes.FrontCover, MetaDataItemType.Image))
                {
                    var libraryItems = LibraryHierarchyBrowser.GetItems(libraryHierarchyNode);
                    if (libraryItems.Any())
                    {
                        var fileNames = await OnDemandMetaDataProvider.GetMetaData(
                            libraryItems,
                            new OnDemandMetaDataRequest(
                                CommonImageTypes.FrontCover,
                                MetaDataItemType.Image,
                                MetaDataUpdateType.System
                            )
                        ).ConfigureAwait(false);
                        metaDataItems = fileNames.Select(fileName => new MetaDataItem(fileName, MetaDataItemType.Image)
                        {
                            Name = CommonImageTypes.FrontCover
                        }).ToArray();
                    }
                }
            }
            return new FileData(libraryHierarchyNode, metaDataItems);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public static readonly LibraryHierarchyNodeConverter Instance = new LibraryHierarchyNodeConverter();

        public class FileData : PersistableComponent, IFileData
        {
            public FileData(LibraryHierarchyNode libraryHierarchyNode, IList<MetaDataItem> metaDataItems)
            {
                this.Id = libraryHierarchyNode.Id;
                this.LibraryHierarchyNode = libraryHierarchyNode;
                this.MetaDataItems = metaDataItems;
            }

            public LibraryHierarchyNode LibraryHierarchyNode { get; private set; }

            public IList<MetaDataItem> MetaDataItems { get; private set; }

            public string DirectoryName
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public string FileName
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }

            public IList<MetaDataItem> MetaDatas
            {
                get
                {
                    return this.MetaDataItems;
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
