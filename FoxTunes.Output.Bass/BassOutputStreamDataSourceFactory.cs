using FoxTunes.Interfaces;

namespace FoxTunes
{
    public class BassOutputStreamDataSourceFactory : StandardFactory, IOutputStreamDataSourceFactory
    {
        public IOutputStreamDataSource Create(IOutputStream outputStream)
        {
            return new BassOutputStreamDataSource(outputStream);
        }
    }
}
