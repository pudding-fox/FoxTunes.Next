﻿using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Tempo : ViewModelBase
    {
        public Tempo()
        {
            this.WindowState = new WindowState(TempoWindow.ID);
        }

        public WindowState WindowState { get; private set; }

        public bool Available
        {
            get
            {
                if (this.Effects == null || this.Effects.Tempo == null || !this.Effects.Tempo.Available)
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
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return false;
                }
                return this.Effects.Tempo.Enabled;
            }
            set
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return;
                }
                this.Effects.Tempo.Enabled = value;
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

        public int MinValue
        {
            get
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return 0;
                }
                return this.Effects.Tempo.MinValue;
            }
        }

        protected virtual void OnMinValueChanged()
        {
            if (this.MinValueChanged != null)
            {
                this.MinValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MinValue");
        }

        public event EventHandler MinValueChanged;

        public int MaxValue
        {
            get
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return 0;
                }
                return this.Effects.Tempo.MaxValue;
            }
        }

        protected virtual void OnMaxValueChanged()
        {
            if (this.MaxValueChanged != null)
            {
                this.MaxValueChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MaxValue");
        }

        public event EventHandler MaxValueChanged;

        public int Value
        {
            get
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return 0;
                }
                return this.Effects.Tempo.Value;
            }
            set
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return;
                }
                this.Effects.Tempo.Value = value;
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

        public ICommand ResetValueCommand
        {
            get
            {
                return new Command(() => this.Value = 0);
            }
        }

        public int MinPitch
        {
            get
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return 0;
                }
                return this.Effects.Tempo.MinPitch;
            }
        }

        protected virtual void OnMinPitchChanged()
        {
            if (this.MinPitchChanged != null)
            {
                this.MinPitchChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MinPitch");
        }

        public event EventHandler MinPitchChanged;

        public int MaxPitch
        {
            get
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return 0;
                }
                return this.Effects.Tempo.MaxPitch;
            }
        }

        protected virtual void OnMaxPitchChanged()
        {
            if (this.MaxPitchChanged != null)
            {
                this.MaxPitchChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MaxPitch");
        }

        public event EventHandler MaxPitchChanged;

        public int Pitch
        {
            get
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return 0;
                }
                return this.Effects.Tempo.Pitch;
            }
            set
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return;
                }
                this.Effects.Tempo.Pitch = value;
            }
        }

        protected virtual void OnPitchChanged()
        {
            if (this.PitchChanged != null)
            {
                this.PitchChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Pitch");
        }

        public event EventHandler PitchChanged;

        public ICommand ResetPitchCommand
        {
            get
            {
                return new Command(() => this.Pitch = 0);
            }
        }

        public int MinRate
        {
            get
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return 0;
                }
                return this.Effects.Tempo.MinRate;
            }
        }

        protected virtual void OnMinRateChanged()
        {
            if (this.MinRateChanged != null)
            {
                this.MinRateChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MinRate");
        }

        public event EventHandler MinRateChanged;

        public int MaxRate
        {
            get
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return 0;
                }
                return this.Effects.Tempo.MaxRate;
            }
        }

        protected virtual void OnMaxRateChanged()
        {
            if (this.MaxRateChanged != null)
            {
                this.MaxRateChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("MaxRate");
        }

        public event EventHandler MaxRateChanged;

        public int Rate
        {
            get
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return 0;
                }
                return this.Effects.Tempo.Rate;
            }
            set
            {
                if (this.Effects == null || this.Effects.Tempo == null)
                {
                    return;
                }
                this.Effects.Tempo.Rate = value;
            }
        }

        protected virtual void OnRateChanged()
        {
            if (this.RateChanged != null)
            {
                this.RateChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Rate");
        }

        public event EventHandler RateChanged;

        public ICommand ResetRateCommand
        {
            get
            {
                return new Command(() => this.Rate = 0);
            }
        }

        public IOutputEffects Effects { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.Effects = core.Components.OutputEffects;
            if (this.Effects.Tempo != null)
            {
                this.Effects.Tempo.AvailableChanged += this.OnAvailableChanged;
                this.Effects.Tempo.EnabledChanged += this.OnEnabledChanged;
                this.Effects.Tempo.ValueChanged += this.OnValueChanged;
                this.Effects.Tempo.PitchChanged += this.OnPitchChanged;
                this.Effects.Tempo.RateChanged += this.OnRateChanged;
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
            var task = Windows.Invoke(this.OnValueChanged);
        }

        protected virtual void OnPitchChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnPitchChanged);
        }

        protected virtual void OnRateChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnRateChanged);
        }

        protected virtual Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                this.OnAvailableChanged();
                this.OnEnabledChanged();
                this.OnMinValueChanged();
                this.OnMaxValueChanged();
                this.OnValueChanged();
                this.OnMinPitchChanged();
                this.OnMaxPitchChanged();
                this.OnPitchChanged();
                this.OnMinRateChanged();
                this.OnMaxRateChanged();
                this.OnRateChanged();
            });
        }

        public ICommand ResetCommand
        {
            get
            {
                return new Command(this.Reset);
            }
        }

        public void Reset()
        {
            this.Value = 0;
            this.Pitch = 0;
            this.Rate = 0;
        }

        protected override void OnDisposing()
        {
            if (this.Effects != null && this.Effects.Tempo != null)
            {
                this.Effects.Tempo.AvailableChanged -= this.OnAvailableChanged;
                this.Effects.Tempo.EnabledChanged -= this.OnEnabledChanged;
                this.Effects.Tempo.ValueChanged -= this.OnValueChanged;
                this.Effects.Tempo.PitchChanged -= this.OnPitchChanged;
                this.Effects.Tempo.RateChanged -= this.OnRateChanged;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Tempo();
        }
    }
}
