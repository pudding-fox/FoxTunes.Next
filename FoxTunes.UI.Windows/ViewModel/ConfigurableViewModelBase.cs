using FoxTunes.Interfaces;
using System;

namespace FoxTunes.ViewModel
{
    public abstract class ConfigurableViewModelBase : ViewModelBase, IConfigurationBaseTarget
    {
        protected ConfigurableViewModelBase(bool initialize = true) : base(initialize)
        {

        }

        private IConfigurationBase _Configuration { get; set; }

        public IConfigurationBase Configuration
        {
            get
            {
                return this._Configuration;
            }
            set
            {
                this._Configuration = value;
                this.OnConfigurationChanged();
            }
        }

        protected virtual void OnConfigurationChanged()
        {
            if (this.ConfigurationChanged != null)
            {
                this.ConfigurationChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Configuration");
        }

        public event EventHandler ConfigurationChanged;
    }
}
