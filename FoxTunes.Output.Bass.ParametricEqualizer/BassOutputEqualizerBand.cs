using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    public class BassOutputEqualizerBand : BaseComponent, IOutputEqualizerBand, IDisposable
    {
        public BassOutputEqualizerBand(string id, int position, float center)
        {
            this.Id = id;
            this.Position = position;
            this.Center = center;
        }

        public string Id { get; private set; }

        public int Position { get; private set; }

        public float MinCenter
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public float MaxCenter
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        private float _Center { get; set; }

        public float Center
        {
            get
            {
                return this._Center;
            }
            set
            {
                this._Center = value;
                this.OnCenterChanged();
            }
        }

        protected virtual void OnCenterChanged()
        {
            if (this.CenterChanged != null)
            {
                this.CenterChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Center");
        }

        public event EventHandler CenterChanged;

        public float MinWidth
        {
            get
            {
                return PeakEQ.MIN_BANDWIDTH;
            }
        }

        public float MaxWidth
        {
            get
            {
                return PeakEQ.MAX_BANDWIDTH;
            }
        }

        public float Width
        {
            get
            {
                return Convert.ToSingle(this.WidthElement.Value);
            }
            set
            {
                this.WidthElement.Value = value;
            }
        }

        protected virtual void OnWidthChanged()
        {
            if (this.WidthChanged != null)
            {
                this.WidthChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Width");
        }

        public event EventHandler WidthChanged;

        public float MinValue
        {
            get
            {
                return PeakEQ.MIN_GAIN;
            }
        }

        public float MaxValue
        {
            get
            {
                return PeakEQ.MAX_GAIN;
            }
        }

        public float Value
        {
            get
            {
                return Convert.ToSingle(this.ValueElement.Value);
            }
            set
            {
                this.ValueElement.Value = value;
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

        public IConfiguration Configuration { get; private set; }

        public DoubleConfigurationElement WidthElement { get; private set; }

        public DoubleConfigurationElement ValueElement { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.WidthElement = this.Configuration.GetElement<DoubleConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassParametricEqualizerStreamComponentConfiguration.BANDWIDTH
            );
            this.WidthElement.ValueChanged += this.OnWidthChanged;
            this.ValueElement = this.Configuration.GetElement<DoubleConfigurationElement>(
                BassOutputConfiguration.SECTION,
                this.Id
            );
            this.ValueElement.ValueChanged += this.OnValueChanged;
            base.InitializeComponent(core);
        }

        protected virtual void OnWidthChanged(object sender, EventArgs e)
        {
            this.OnWidthChanged();
        }

        protected virtual void OnValueChanged(object sender, EventArgs e)
        {
            this.OnValueChanged();
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
            if (this.WidthElement != null)
            {
                this.WidthElement.ValueChanged -= this.OnValueChanged;
            }
            if (this.ValueElement != null)
            {
                this.ValueElement.ValueChanged -= this.OnValueChanged;
            }
        }

        ~BassOutputEqualizerBand()
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
