using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class BassOutputVolume : BassOutputEffect, IOutputVolume, IStandardComponent, IDisposable
    {
        public override bool Available
        {
            get
            {
                //We don't really need BASS_SAMPLE_FLOAT it just helps.
                return true;
            }
            protected set
            {
                //Nothing to do.
            }
        }

        public override bool Enabled
        {
            get
            {
                return this.EnabledElement.Value;
            }
            set
            {
                this.EnabledElement.Value = value;
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

        public BooleanConfigurationElement EnabledElement { get; private set; }

        public DoubleConfigurationElement ValueElement { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            base.InitializeComponent(core);
            this.EnabledElement = this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.VOLUME_ENABLED_ELEMENT
            );
            this.EnabledElement.ValueChanged += this.OnEnabledChanged;
            this.ValueElement = this.Configuration.GetElement<DoubleConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.VOLUME_ELEMENT
            );
            this.ValueElement.ValueChanged += this.OnValueChanged;
        }

        protected virtual void OnEnabledChanged(object sender, EventArgs e)
        {
            this.OnEnabledChanged();
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
            if (this.EnabledElement != null)
            {
                this.EnabledElement.ValueChanged -= this.OnEnabledChanged;
            }
            if (this.ValueElement != null)
            {
                this.ValueElement.ValueChanged -= this.OnValueChanged;
            }
        }

        ~BassOutputVolume()
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
