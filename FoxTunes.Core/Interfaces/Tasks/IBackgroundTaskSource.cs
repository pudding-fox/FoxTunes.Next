using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IBackgroundTaskSource : IBaseComponent
    {
        event BackgroundTaskEventHandler BackgroundTask;
    }

    public delegate Task BackgroundTaskEventHandler(object sender, IBackgroundTask backgroundTask);
}
