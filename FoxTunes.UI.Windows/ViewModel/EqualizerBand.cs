using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class EqualizerBand : ViewModelBase
    {
        public EqualizerBand()
        {

        }

        public EqualizerBand(IOutputEqualizerBand band) : this()
        {
            this.Band = band;
            this.Band.ValueChanged += this.OnValueChanged;
        }

        public IOutputEqualizerBand Band { get; private set; }

        public string Name
        {
            get
            {
                if (this.Band.Center < 1000)
                {
                    return string.Format("{0}Hz", this.Band.Center);
                }
                else
                {
                    return string.Format("{0}kHz", this.Band.Center / 1000);
                }

            }
        }

        public float MinValue
        {
            get
            {
                return this.Band.MinValue;
            }
        }

        public float MaxValue
        {
            get
            {
                return this.Band.MaxValue;
            }
        }

        public float Value
        {
            get
            {
                return this.Band.Value;
            }
            set
            {
                this.Band.Value = value;
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

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            this.Dispatch(this.Refresh);
        }

        public ICommand ResetValueCommand
        {
            get
            {
                return new Command(() => this.Value = 0);
            }
        }

        protected virtual Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                this.OnValueChanged();
            });
        }

        protected override Freezable CreateInstanceCore()
        {
            return new EqualizerBand();
        }
    }
}
