namespace FoxTunes.Interfaces
{
    public interface IOutputStreamDataSourceFactory : IStandardFactory
    {
        IOutputStreamDataSource Create(IOutputStream outputStream);
    }
}
