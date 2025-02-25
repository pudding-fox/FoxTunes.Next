using FoxTunes.Interfaces;
using System;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Shuffle : ViewModelBase
    {
        public IConfiguration Configuration { get; private set; }

        public SelectionConfigurationElement Order { get; private set; }

        public bool Enabled
        {
            get
            {
                if (this.Order == null)
                {
                    return false;
                }
                if (string.Equals(this.Order.Value.Id, PlaylistBehaviourConfiguration.ORDER_DEFAULT_OPTION, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                return true;
            }
            set
            {
                if (this.Order == null)
                {
                    return;
                }
                if (value)
                {
                    this.Order.Value = this.Order.GetOption(PlaylistBehaviourConfiguration.ORDER_SHUFFLE_TRACKS);
                }
                else
                {
                    this.Order.Value = this.Order.GetOption(PlaylistBehaviourConfiguration.ORDER_DEFAULT_OPTION);
                }
            }
        }

        protected virtual void OnEnabledChanged()
        {
            if (this.EnabledChanged != null)
            {
                this.EnabledChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Enabled");
        }

        public event EventHandler EnabledChanged;

        protected override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Order = this.Configuration.GetElement<SelectionConfigurationElement>(
                PlaylistBehaviourConfiguration.SECTION,
                PlaylistBehaviourConfiguration.ORDER_ELEMENT
            );
            this.Order.ValueChanged += this.OnValueChanged;
            this.OnEnabledChanged();
            base.InitializeComponent(core);
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnEnabledChanged);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Shuffle();
        }

        protected override void OnDisposing()
        {
            if (this.Order != null)
            {
                this.Order.ValueChanged -= this.OnValueChanged;
            }
            base.OnDisposing();
        }
    }
}
