using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class Equalizer : ViewModelBase
    {
        public Equalizer()
        {
            this.WindowState = new WindowState(EqualizerWindow.ID);
        }

        public WindowState WindowState { get; private set; }

        public bool Available
        {
            get
            {
                if (this.Effects == null || this.Effects.Equalizer == null || !this.Effects.Equalizer.Available)
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
                if (this.Effects == null || this.Effects.Equalizer == null)
                {
                    return false;
                }
                return this.Effects.Equalizer.Enabled;
            }
            set
            {
                if (this.Effects == null || this.Effects.Equalizer == null)
                {
                    return;
                }
                this.Effects.Equalizer.Enabled = value;
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

        public IEnumerable<EqualizerBand> Bands { get; private set; }

        protected virtual void OnBandsChanged()
        {
            if (this.BandsChanged != null)
            {
                this.BandsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Bands");
        }

        public event EventHandler BandsChanged;

        public IEnumerable<string> Presets
        {
            get
            {
                if (this.Effects == null || this.Effects.Equalizer == null)
                {
                    return null;
                }
                return this.Effects.Equalizer.Presets;
            }
        }

        protected virtual void OnPresetsChanged()
        {
            if (this.PresetsChanged != null)
            {
                this.PresetsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Presets");
        }

        public event EventHandler PresetsChanged;

        public string Preset
        {
            get
            {
                if (this.Effects != null && this.Effects.Equalizer != null)
                {
                    return this.Effects.Equalizer.Preset;
                }
                return string.Empty;
            }
            set
            {
                if (this.Effects != null && this.Effects.Equalizer != null)
                {
                    this.Effects.Equalizer.Preset = value;
                }
            }
        }

        protected virtual void OnPresetChanged()
        {
            if (this.PresetChanged != null)
            {
                this.PresetChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Preset");
        }

        public event EventHandler PresetChanged;

        public IOutputEffects Effects { get; private set; }

        protected override void InitializeComponent(ICore core)
        {

            this.Effects = core.Components.OutputEffects;
            if (this.Effects.Equalizer != null)
            {
                this.Effects.Equalizer.AvailableChanged += this.OnAvailableChanged;
                this.Effects.Equalizer.EnabledChanged += this.OnEnabledChanged;
                this.Effects.Equalizer.PresetsChanged += this.OnPresetsChanged;
                this.Effects.Equalizer.PresetChanged += this.OnPresetChanged;
            }
            this.Bands = new List<EqualizerBand>(this.GetBands());
            foreach (var band in this.Bands)
            {
                band.ValueChanged += this.OnValueChanged;
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

        protected virtual void OnPresetsChanged(object sender, EventArgs e)
        {
            this.Dispatch(this.Refresh);
        }

        protected virtual void OnPresetChanged(object sender, EventArgs e)
        {
            this.Dispatch(this.Refresh);
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.OnPresetChanged);
        }

        protected virtual Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                this.OnAvailableChanged();
                this.OnEnabledChanged();
                this.OnBandsChanged();
                this.OnPresetsChanged();
                this.OnPresetChanged();
            });
        }

        protected virtual IEnumerable<EqualizerBand> GetBands()
        {
            if (this.Effects != null && this.Effects.Equalizer != null)
            {
                foreach (var band in this.Effects.Equalizer.Bands)
                {
                    yield return new EqualizerBand(band);
                }
            }
        }

        protected override void OnDisposing()
        {
            if (this.Effects != null && this.Effects.Equalizer != null)
            {
                this.Effects.Equalizer.AvailableChanged -= this.OnAvailableChanged;
                this.Effects.Equalizer.EnabledChanged -= this.OnEnabledChanged;
                this.Effects.Equalizer.PresetsChanged -= this.OnPresetsChanged;
                this.Effects.Equalizer.PresetChanged -= this.OnPresetChanged;
            }
            if (this.Bands != null)
            {
                foreach (var band in this.Bands)
                {
                    band.ValueChanged -= this.OnValueChanged;
                }
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Equalizer();
        }
    }
}
