using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface ISignalEmitter : ISignalSource
    {
        Task Send(ISignal signal);
    }
}
