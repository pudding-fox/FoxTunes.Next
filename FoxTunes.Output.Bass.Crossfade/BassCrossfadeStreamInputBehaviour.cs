using FoxTunes.Interfaces;
using ManagedBass.Crossfade;
using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class BassCrossfadeStreamInputBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(BassCrossfadeStreamInputBehaviour).Assembly.Location);
            }
        }

        public BassCrossfadeStreamInputBehaviour()
        {
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", BassLoader.DIRECTORY_NAME_ADDON));
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", "bass_crossfade.dll"));
        }

        public ICore Core { get; private set; }

        public IBassOutput Output { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public IBassStreamPipelineFactory BassStreamPipelineFactory { get; private set; }

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
                var task = this.Output.Shutdown();
            }
        }

        public BassCrossfadeMode Mode
        {
            get
            {
                return BassCrossfade.Mode;
            }
            set
            {
                BassCrossfade.Mode = value;
                Logger.Write(this, LogLevel.Debug, "Mode = {0}", Enum.GetName(typeof(BassCrossfadeMode), this.Mode));
            }
        }

        public int InPeriod
        {
            get
            {
                return BassCrossfade.InPeriod;
            }
            set
            {
                BassCrossfade.InPeriod = value;
                Logger.Write(this, LogLevel.Debug, "InPeriod = {0}", this.InPeriod);
            }
        }

        public int OutPeriod
        {
            get
            {
                return BassCrossfade.OutPeriod;
            }
            set
            {
                BassCrossfade.OutPeriod = value;
                Logger.Write(this, LogLevel.Debug, "OutPeriod = {0}", this.OutPeriod);
            }
        }

        public BassCrossfadeType InType
        {
            get
            {
                return BassCrossfade.InType;
            }
            set
            {
                BassCrossfade.InType = value;
                Logger.Write(this, LogLevel.Debug, "InType = {0}", Enum.GetName(typeof(BassCrossfadeType), this.InType));
            }
        }

        public BassCrossfadeType OutType
        {
            get
            {
                return BassCrossfade.OutType;
            }
            set
            {
                BassCrossfade.OutType = value;
                Logger.Write(this, LogLevel.Debug, "OutType = {0}", Enum.GetName(typeof(BassCrossfadeType), this.OutType));
            }
        }

        public bool Mix
        {
            get
            {
                return BassCrossfade.Mix;
            }
            set
            {
                BassCrossfade.Mix = value;
                Logger.Write(this, LogLevel.Debug, "Mix = {0}", this.Mix);
            }
        }

        private bool _Start { get; set; }

        public bool Start
        {
            get
            {
                return this._Start;
            }
            set
            {
                this._Start = value;
                Logger.Write(this, LogLevel.Debug, "Start = {0}", this.Start);
            }
        }

        private bool _PauseResume { get; set; }

        public bool PauseResume
        {
            get
            {
                return this._PauseResume;
            }
            set
            {
                this._PauseResume = value;
                Logger.Write(this, LogLevel.Debug, "Pause/Resume = {0}", this.PauseResume);
            }
        }

        private bool _Stop { get; set; }

        public bool Stop
        {
            get
            {
                return this._Stop;
            }
            set
            {
                this._Stop = value;
                Logger.Write(this, LogLevel.Debug, "Stop = {0}", this.Stop);
            }
        }

        public bool Buffer
        {
            get
            {
                return BassCrossfade.Buffer;
            }
            set
            {
                BassCrossfade.Buffer = value;
                Logger.Write(this, LogLevel.Debug, "Buffer = {0}", this.Mix);
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.Output = core.Components.Output as IBassOutput;
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassOutputConfiguration.INPUT_ELEMENT
            ).ConnectValue(value => this.Enabled = string.Equals(value.Id, BassCrossfadeStreamInputConfiguration.INPUT_CROSSFADE_OPTION, StringComparison.OrdinalIgnoreCase));
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassCrossfadeStreamInputConfiguration.MODE_ELEMENT
            ).ConnectValue(option => this.Mode = BassCrossfadeStreamInputConfiguration.GetMode(option));
            this.Configuration.GetElement<IntegerConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassCrossfadeStreamInputConfiguration.PERIOD_IN_ELEMENT
            ).ConnectValue(value => this.InPeriod = value);
            this.Configuration.GetElement<IntegerConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassCrossfadeStreamInputConfiguration.PERIOD_OUT_ELEMENT
            ).ConnectValue(value => this.OutPeriod = value);
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassCrossfadeStreamInputConfiguration.TYPE_IN_ELEMENT
            ).ConnectValue(option => this.InType = BassCrossfadeStreamInputConfiguration.GetType(option));
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassCrossfadeStreamInputConfiguration.TYPE_OUT_ELEMENT
            ).ConnectValue(option => this.OutType = BassCrossfadeStreamInputConfiguration.GetType(option));
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassCrossfadeStreamInputConfiguration.MIX_ELEMENT
            ).ConnectValue(value => this.Mix = value);
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassCrossfadeStreamInputConfiguration.START_ELEMENT
            ).ConnectValue(value => this.Start = value);
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassCrossfadeStreamInputConfiguration.PAUSE_RESUME_ELEMENT
            ).ConnectValue(value => this.PauseResume = value);
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassCrossfadeStreamInputConfiguration.STOP_ELEMENT
            ).ConnectValue(value => this.Stop = value);
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassCrossfadeStreamInputConfiguration.BUFFER_ELEMENT
            ).ConnectValue(value => this.Buffer = value);
            this.BassStreamPipelineFactory = ComponentRegistry.Instance.GetComponent<IBassStreamPipelineFactory>();
            this.BassStreamPipelineFactory.CreatingPipeline += this.OnCreatingPipeline;
            base.InitializeComponent(core);
        }

        protected virtual void OnCreatingPipeline(object sender, CreatingPipelineEventArgs e)
        {
            if (!this.Enabled || e.Input != null)
            {
                return;
            }
            if (!BassCrossfadeStreamInput.CanCreate(this, e.Stream, e.Query))
            {
                Logger.Write(this, LogLevel.Warn, "Cannot create input, the stream is not supported.");
                throw new InvalidOperationException(Strings.BassCrossfadeStreamInputBehaviour_Unsupported);
            }
            e.Input = new BassCrossfadeStreamInput(this, e.Pipeline, e.Stream.Flags);
            e.Input.InitializeComponent(this.Core);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BassCrossfadeStreamInputConfiguration.GetConfigurationSections();
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
            if (this.BassStreamPipelineFactory != null)
            {
                this.BassStreamPipelineFactory.CreatingPipeline -= this.OnCreatingPipeline;
            }
        }

        ~BassCrossfadeStreamInputBehaviour()
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