using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface ISignalSource : IStandardComponent
    {
        event SignalEventHandler Signal;
    }

    public delegate Task SignalEventHandler(object sender, ISignal signal);
}
