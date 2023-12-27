using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    [ComponentPriority(ComponentPriorityAttribute.HIGH)]
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class BassDirectSoundStreamOutputBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        public ICore Core { get; private set; }

        public IBassOutput Output { get; private set; }

        public IOutputDeviceManager OutputDeviceManager { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public IBassStreamPipelineFactory BassStreamPipelineFactory { get; private set; }

        new public bool IsInitialized { get; private set; }

        private bool _Enabled { get; set; }

        public bool Enabled
        {
            get
            {
                return this._Enabled;
            }
            set
            {
                this._Enabled = value;
                Logger.Write(this, LogLevel.Debug, "Enabled = {0}", this.Enabled);
                this.OutputDeviceManager.Restart();
            }
        }

        private int _DirectSoundDevice { get; set; }

        public int DirectSoundDevice
        {
            get
            {
                return this._DirectSoundDevice;
            }
            set
            {
                this._DirectSoundDevice = value;
                Logger.Write(this, LogLevel.Debug, "Direct Sound Device = {0}", this.DirectSoundDevice);
                this.OutputDeviceManager.Restart();
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.Output = core.Components.Output as IBassOutput;
            this.Output.Init += this.OnInit;
            this.Output.Free += this.OnFree;
            this.OutputDeviceManager = core.Managers.OutputDevice;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.OUTPUT_ELEMENT
            ).ConnectValue(value => this.Enabled = string.Equals(value.Id, BassDirectSoundStreamOutputConfiguration.OUTPUT_DS_OPTION, StringComparison.OrdinalIgnoreCase));
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassDirectSoundStreamOutputConfiguration.ELEMENT_DS_DEVICE
            ).ConnectValue(value => this.DirectSoundDevice = BassDirectSoundStreamOutputConfiguration.GetDsDevice(value));
            this.BassStreamPipelineFactory = ComponentRegistry.Instance.GetComponent<IBassStreamPipelineFactory>();
            this.BassStreamPipelineFactory.QueryingPipeline += this.OnQueryingPipeline;
            this.BassStreamPipelineFactory.CreatingPipeline += this.OnCreatingPipeline;
            base.InitializeComponent(core);
        }

        protected virtual void OnInit(object sender, EventArgs e)
        {
            if (!this.Enabled)
            {
                return;
            }
            this.IsInitialized = true;
            BassUtils.OK(Bass.Configure(global::ManagedBass.Configuration.UpdatePeriod, this.Output.UpdatePeriod));
            BassUtils.OK(Bass.Configure(global::ManagedBass.Configuration.UpdateThreads, this.Output.UpdateThreads));
            BassUtils.OK(Bass.Configure(global::ManagedBass.Configuration.PlaybackBufferLength, this.Output.BufferLength));
            BassUtils.OK(Bass.Configure(global::ManagedBass.Configuration.MixerBufferLength, this.Output.MixerBufferLength));
            BassUtils.OK(Bass.Configure(global::ManagedBass.Configuration.SRCQuality, this.Output.ResamplingQuality));
            BassUtils.OK(Bass.Init(this.DirectSoundDevice, this.Output.Rate));
            Logger.Write(this, LogLevel.Debug, "BASS Initialized.");
        }

        protected virtual void OnInitDevice()
        {
            if (BassDirectSoundDevice.IsInitialized)
            {
                return;
            }
            BassDirectSoundDevice.Init(this.DirectSoundDevice);
        }

        protected virtual void OnFree(object sender, EventArgs e)
        {
            if (!this.IsInitialized)
            {
                return;
            }
            this.OnFreeDevice();
            LogManager.Logger.Write(this, LogLevel.Debug, "Releasing BASS.");
            Bass.Free();
            this.IsInitialized = false;
        }

        protected virtual void OnFreeDevice()
        {
            if (!BassDirectSoundDevice.IsInitialized)
            {
                return;
            }
            BassDirectSoundDevice.Free();
        }

        protected virtual void OnQueryingPipeline(object sender, QueryingPipelineEventArgs e)
        {
            if (!this.Enabled)
            {
                return;
            }
            this.OnInitDevice();
            e.OutputRates = BassDirectSoundDevice.Info.SupportedRates;
            e.OutputChannels = BassDirectSoundDevice.Info.Outputs;
        }

        protected virtual void OnCreatingPipeline(object sender, CreatingPipelineEventArgs e)
        {
            if (!this.Enabled)
            {
                return;
            }
            var output = new BassDirectSoundStreamOutput(this, e.Pipeline, e.Stream.Flags);
            output.InitializeComponent(this.Core);
            e.Output = output;
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BassDirectSoundStreamOutputConfiguration.GetConfigurationSections();
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
            if (this.Output != null)
            {
                this.Output.Init -= this.OnInit;
                this.Output.Free -= this.OnFree;
            }
            if (this.BassStreamPipelineFactory != null)
            {
                this.BassStreamPipelineFactory.QueryingPipeline -= this.OnQueryingPipeline;
                this.BassStreamPipelineFactory.CreatingPipeline -= this.OnCreatingPipeline;
            }
        }

        ~BassDirectSoundStreamOutputBehaviour()
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
