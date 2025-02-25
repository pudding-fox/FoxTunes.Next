using FoxTunes.Interfaces;

namespace FoxTunes
{
    public abstract class MessageSinkFactory : BaseFactory, IMessageSinkFactory
    {
        public abstract IMessageSink Create(uint id);
    }
}
