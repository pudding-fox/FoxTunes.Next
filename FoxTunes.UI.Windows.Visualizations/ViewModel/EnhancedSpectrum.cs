using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class EnhancedSpectrum : ConfigurableViewModelBase
    {
        public StringCollection Levels
        {
            get
            {
                var levels = new List<string>();
                for (var level = EnhancedSpectrumRenderer.DB_MIN; level <= EnhancedSpectrumRenderer.DB_MAX; level += 10)
                {
                    levels.Add(Convert.ToString(level));
                }
                return new StringCollection(levels);
            }
        }

        private StringCollection _Bands { get; set; }

        public StringCollection Bands
        {
            get
            {
                return this._Bands;
            }
            private set
            {
                this._Bands = value;
                this.OnBandsChanged();
            }
        }

        protected virtual void OnBandsChanged()
        {
            if (this.BandsChanged != null)
            {
                this.BandsChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Bands");
        }

        public event EventHandler BandsChanged;

        public SelectionConfigurationElement BandsElement { get; private set; }

        public TextConfigurationElement CustomElement { get; private set; }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.BandsElement = this.Configuration.GetElement<SelectionConfigurationElement>(
                    EnhancedSpectrumConfiguration.SECTION,
                    EnhancedSpectrumConfiguration.BANDS_ELEMENT
                );
                this.CustomElement = this.Configuration.GetElement<TextConfigurationElement>(
                    EnhancedSpectrumConfiguration.SECTION,
                    EnhancedSpectrumConfiguration.BANDS_CUSTOM_ELEMENT
                );
                this.BandsElement.ValueChanged += this.OnValueChanged;
                this.CustomElement.ValueChanged += this.OnValueChanged;
                this.Refresh();
            }
            base.OnConfigurationChanged();
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            this.Refresh();
        }

        public void Refresh()
        {
            var bands = EnhancedSpectrumConfiguration.GetBands(this.BandsElement.Value, this.CustomElement).Select(
                band => band < 1000 ? Convert.ToString(band) : string.Format("{0:0.##}K", (float)band / 1000)
            );
            this.Bands = new StringCollection(bands);
        }

        protected override Freezable CreateInstanceCore()
        {
            return new EnhancedSpectrum();
        }

        protected override void OnDisposing()
        {
            if (this.BandsElement != null)
            {
                this.BandsElement.ValueChanged -= this.OnValueChanged;
            }
            if (this.CustomElement != null)
            {
                this.CustomElement.ValueChanged -= this.OnValueChanged;
            }
            base.OnDisposing();
        }
    }
}
