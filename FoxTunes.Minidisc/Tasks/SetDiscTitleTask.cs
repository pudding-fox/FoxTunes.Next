using FoxTunes.Interfaces;
using MD.Net;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class SetDiscTitleTask : MinidiscActionTask
    {
        public SetDiscTitleTask(IDevice device, string title)
        {
            this.Device = device;
            this.Title = title;
        }

        public IDevice Device { get; private set; }

        public string Title { get; private set; }

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
                Logger.Write(this, LogLevel.Debug, "Setting title: {0}", this.Title);
                var updatedDisc = currentDisc.Clone();
                updatedDisc.Title = this.Title;
                var actions = this.ActionBuilder.GetActions(this.Device, currentDisc, updatedDisc);
                this.ApplyActions(this.Device, actions);
                Logger.Write(this, LogLevel.Debug, "Successfully set title.");
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
