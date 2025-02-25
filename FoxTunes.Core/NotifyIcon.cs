using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public abstract class NotifyIcon : BaseComponent, INotifyIcon
    {
        public abstract IMessageSink MessageSink { get; protected set; }

        public abstract IntPtr Icon { get; set; }

        public abstract void Show();

        public abstract bool Update();

        public abstract void Hide();

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return NotifyIconConfiguration.GetConfigurationSections();
        }

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
            this.Hide();
        }

        ~NotifyIcon()
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
