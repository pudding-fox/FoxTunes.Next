using FoxTunes.Interfaces;

namespace FoxTunes
{
    public class StandardFactories : IStandardFactories
    {
        public IDatabaseFactory Database
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IDatabaseFactory>();
            }
        }

        public IMetaDataSourceFactory MetaDataSource
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IMetaDataSourceFactory>();
            }
        }

        public IMetaDataDecoratorFactory MetaDataDecorator
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IMetaDataDecoratorFactory>();
            }
        }

        public IOutputStreamDataSourceFactory OutputStreamDataSource
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IOutputStreamDataSourceFactory>();
            }
        }

        public IFFTDataTransformerFactory FFTDataTransformer
        {
            get
            {
                return ComponentRegistry.Instance.GetComponent<IFFTDataTransformerFactory>();
            }
        }

        public static readonly IStandardFactories Instance = new StandardFactories();
    }
}
