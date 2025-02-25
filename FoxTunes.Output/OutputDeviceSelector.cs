using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class OutputDeviceSelector : StandardComponent, IOutputDeviceSelector, IDisposable
    {
        public abstract string Name { get; }

        public abstract bool IsActive { get; set; }

        protected virtual void OnIsActiveChanged()
        {
            if (this.IsActiveChanged != null)
            {
                this.IsActiveChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsActive");
        }

        public event EventHandler IsActiveChanged;

        public abstract IEnumerable<OutputDevice> Devices { get; }

        protected virtual void OnDevicesChanged()
        {
            if (this.DevicesChanged != null)
            {
                this.DevicesChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Devices");
        }

        public event EventHandler DevicesChanged;

        public abstract OutputDevice Device { get; set; }

        protected virtual void OnDeviceChanged()
        {
            if (this.DeviceChanged != null)
            {
                this.DeviceChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Device");
        }

        public event EventHandler DeviceChanged;

        public abstract void Refresh();

        public abstract Task ShowSettings();

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            //Nothing to do.
        }

        ~OutputDeviceSelector()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }
    }
}
