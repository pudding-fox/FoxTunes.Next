using FoxTunes.Interfaces;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class BackgroundTaskEmitter : StandardComponent, IBackgroundTaskEmitter
    {
        public Task Send(IBackgroundTask backgroundTask)
        {
            return this.OnBackgroundTask(backgroundTask);
        }

        protected virtual Task OnBackgroundTask(IBackgroundTask backgroundTask)
        {
            if (this.BackgroundTask == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.BackgroundTask(this, backgroundTask);
        }

        public event BackgroundTaskEventHandler BackgroundTask;
    }
}
