using FoxTunes.Interfaces;
using MD.Net;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class EraseMinidiscTask : MinidiscActionTask
    {
        public EraseMinidiscTask(IDevice device)
        {
            this.Device = device;
        }

        public IDevice Device { get; private set; }

        protected override Task OnStarted()
        {
            this.Name = Strings.EraseMinidiscTask_Name;
            return base.OnStarted();
        }

        protected override Task OnRun()
        {
            Logger.Write(this, LogLevel.Debug, "Reading disc..");
            var currentDisc = this.DiscManager.GetDisc(this.Device);
            if (currentDisc != null)
            {
                Logger.Write(this, LogLevel.Debug, "Current disc \"{0}\" has {1} tracks.", currentDisc.Title, currentDisc.Tracks.Count);
                this.Name = string.Format("{0}: {1}", Strings.EraseMinidiscTask_Name, currentDisc.Title);
                Logger.Write(this, LogLevel.Debug, "Erasing..");
                var updatedDisc = currentDisc.Clone();
                updatedDisc.Title = string.Empty;
                updatedDisc.Tracks.Clear();
                var actions = this.ActionBuilder.GetActions(this.Device, currentDisc, updatedDisc);
                this.ApplyActions(this.Device, actions);
                Logger.Write(this, LogLevel.Debug, "Successfully erased disc.");
            }
            else
            {
                Logger.Write(this, LogLevel.Warn, "Disc could not be read.");
                throw new Exception(Strings.MinidiscTask_NoDisc);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
