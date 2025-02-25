using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    public abstract class OutputEffect : BaseComponent, IOutputEffect
    {
        public abstract bool Available { get; protected set; }

        protected virtual void OnAvailableChanged()
        {
            if (this.AvailableChanged != null)
            {
                this.AvailableChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Available");
        }

        public event EventHandler AvailableChanged;

        public abstract bool Enabled { get; set; }

        protected virtual void OnEnabledChanged()
        {
            if (this.EnabledChanged != null)
            {
                this.EnabledChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Enabled");
        }

        public event EventHandler EnabledChanged;
    }
}
