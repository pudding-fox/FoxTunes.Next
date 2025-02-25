using FoxTunes.Interfaces;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class BassOutputStreamDataSourceFactory : StandardFactory, IOutputStreamDataSourceFactory
    {
        public IOutputStreamDataSource Create(IOutputStream outputStream)
        {
            return new BassOutputStreamDataSource(outputStream);
        }
    }
}
