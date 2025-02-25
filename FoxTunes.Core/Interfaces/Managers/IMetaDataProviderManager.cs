namespace FoxTunes.Interfaces
{
    public interface IMetaDataProviderManager : IStandardManager, IDatabaseInitializer
    {
        IMetaDataProvider GetProvider(MetaDataProvider metaDataProvider);

        MetaDataProvider[] GetProviders();
    }
}
