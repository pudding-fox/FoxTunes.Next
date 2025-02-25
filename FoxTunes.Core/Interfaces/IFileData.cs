using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IFileData : IPersistableComponent
    {
        string DirectoryName { get; set; }

        string FileName { get; set; }

        IList<MetaDataItem> MetaDatas { get; set; }
    }
}
