using FoxTunes.Interfaces;
using MD.Net;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class OpenMinidiscTask : MinidiscTask
    {
        public IDevice Device { get; private set; }

        public IDisc Disc { get; private set; }

        protected override Task OnStarted()
        {
            this.Name = Strings.OpenMinidiscTask_Name;
            return base.OnStarted();
        }

        protected override Task OnRun()
        {
            Logger.Write(this, LogLevel.Debug, "Checking device..");
            this.Device = this.DeviceManager.GetDevices().FirstOrDefault();
            if (this.Device != null)
            {
                Logger.Write(this, LogLevel.Debug, "Current device: {0}", this.Device.Name);
                this.Name = string.Format("{0}: {1}", Strings.OpenMinidiscTask_Name, this.Device.Name);
                Logger.Write(this, LogLevel.Debug, "Reading disc..");
                this.Disc = this.DiscManager.GetDisc(this.Device);
                if (this.Disc != null)
                {
                    Logger.Write(this, LogLevel.Debug, "Current disc \"{0}\" has {1} tracks.", this.Disc.Title, this.Disc.Tracks.Count);
                }
                else
                {
                    Logger.Write(this, LogLevel.Warn, "Disc could not be read.");
                }
            }
            else
            {
                Logger.Write(this, LogLevel.Warn, "Device not found.");
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
