using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class Volume : ViewModelBase
    {
        public bool Available
        {
            get
            {
                if (this.Effects == null || this.Effects.Volume == null || !this.Effects.Volume.Available)
                {
                    return false;
                }
                return true;
            }
        }

        protected virtual void OnAvailableChanged()
        {
            if (this.AvailableChanged != null)
            {
                this.AvailableChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Available");
        }

        public event EventHandler AvailableChanged;

        public bool Enabled
        {
            get
            {
                if (this.Effects == null || this.Effects.Volume == null)
                {
                    return false;
                }
                return this.Effects.Volume.Enabled;
            }
            set
            {
                if (this.Effects == null || this.Effects.Volume == null)
                {
                    return;
                }
                this.Effects.Volume.Enabled = value;
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

        public float Value
        {
            get
            {
                if (this.Effects == null || this.Effects.Volume == null)
                {
                    return 0;
                }
                return this.Effects.Volume.Value;
            }
            set
            {
                if (this.Effects == null || this.Effects.Volume == null)
                {
                    return;
                }
                this.Effects.Volume.Value = value;
            }
        }

        protected virtual void OnValueChanged()
        {
            if (this.ValueChanged != null)
            {
                this.ValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Value");
        }

        public event EventHandler ValueChanged;

        public IOutputEffects Effects { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.Effects = core.Components.OutputEffects;
            if (this.Effects.Volume != null)
            {
                this.Effects.Volume.AvailableChanged += this.OnAvailableChanged;
                this.Effects.Volume.EnabledChanged += this.OnEnabledChanged;
                this.Effects.Volume.ValueChanged += this.OnValueChanged;
            }
            this.Dispatch(this.Refresh);
            base.InitializeComponent(core);
        }

        protected virtual void OnAvailableChanged(object sender, EventArgs e)
        {
            this.Dispatch(this.Refresh);
        }

        protected virtual void OnEnabledChanged(object sender, EventArgs e)
        {
            this.Dispatch(this.Refresh);
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            this.Dispatch(this.Refresh);
        }

        protected virtual Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                this.OnAvailableChanged();
                this.OnEnabledChanged();
                this.OnValueChanged();
            });
        }

        protected override void OnDisposing()
        {
            if (this.Effects != null && this.Effects.Volume != null)
            {
                this.Effects.Volume.AvailableChanged -= this.OnAvailableChanged;
                this.Effects.Volume.EnabledChanged -= this.OnEnabledChanged;
                this.Effects.Volume.ValueChanged -= this.OnValueChanged;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Volume();
        }
    }
}
