using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IBackgroundTaskEmitter : IBackgroundTaskSource
    {
        Task Send(IBackgroundTask backgroundTask);
    }
}
