using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IMetaDataProviderCache : IStandardComponent
    {
        MetaDataProvider[] GetProviders(Func<IEnumerable<MetaDataProvider>> factory);
    }
}
