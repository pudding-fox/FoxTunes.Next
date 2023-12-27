namespace FoxTunes.Interfaces
{
    public interface IMessageSinkFactory : IBaseFactory
    {
        IMessageSink Create(uint id);
    }
}
