using FoxTunes.Interfaces;
using MD.Net;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class WriteMinidiscTask : MinidiscActionTask
    {
        public WriteMinidiscTask(IDevice device, IActions actions)
        {
            this.Device = device;
            this.Actions = actions;
        }

        public IDevice Device { get; private set; }

        public IActions Actions { get; private set; }

        protected override Task OnStarted()
        {
            this.Name = Strings.WriteMinidiscTask_Name;
            return base.OnStarted();
        }

        protected override Task OnRun()
        {
            Logger.Write(this, LogLevel.Debug, "Writing disc..");
            this.ApplyActions(this.Device, this.Actions);
            Logger.Write(this, LogLevel.Debug, "Successfully wrote disc.");
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
