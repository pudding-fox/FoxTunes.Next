using FoxTunes.Interfaces;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class WindowsMessageSinkFactory : MessageSinkFactory, IStandardFactory
    {
        public override IMessageSink Create(uint id)
        {
            return new WindowsMessageSink(id);
        }
    }
}
