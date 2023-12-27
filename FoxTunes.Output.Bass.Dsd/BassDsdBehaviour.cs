using FoxTunes.Interfaces;
using ManagedBass.Dsd;
using ManagedBass.Memory;
using System;
using System.Collections.Generic;
using System.IO;

namespace FoxTunes
{
    public class BassDsdBehaviour : StandardBehaviour, IConfigurableComponent
    {
        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(BassDsdBehaviour).Assembly.Location);
            }
        }

        public static readonly string[] EXTENSIONS = new[]
        {
            "dff",
            "dsd",
            "dsf"
        };

        public BassDsdBehaviour()
        {
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", BassLoader.DIRECTORY_NAME_ADDON));
            BassLoader.AddPath(Path.Combine(Location, Environment.Is64BitProcess ? "x64" : "x86", "bass_memory_dsd.dll"));
            //bassdsd.dll does not register all possible extensions.
            BassLoader.AddExtensions(EXTENSIONS);
        }

        public ICore Core { get; private set; }

        public IBassStreamPipelineFactory BassStreamPipelineFactory { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public int Rate
        {
            get
            {
                return BassDsd.DefaultFrequency;
            }
            set
            {
                BassDsd.DefaultFrequency = value;
                Logger.Write(this, LogLevel.Debug, "DSD to PCM sample rate: {0}", MetaDataInfo.SampleRateDescription(BassDsd.DefaultFrequency));
            }
        }

        public int Gain
        {
            get
            {
                return BassDsd.DefaultGain;
            }
            set
            {
                BassDsd.DefaultGain = value;
                Logger.Write(this, LogLevel.Debug, "DSD to PCM gain: {0}{1}dB", BassDsd.DefaultGain > 0 ? "+" : string.Empty, BassDsd.DefaultGain);
            }
        }

        private bool _Memory { get; set; }

        public bool Memory
        {
            get
            {
                return this._Memory;
            }
            set
            {
                this._Memory = value;
                Logger.Write(this, LogLevel.Debug, "Play DSD from memory: {0}", value ? bool.TrueString : bool.FalseString);
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.BassStreamPipelineFactory = ComponentRegistry.Instance.GetComponent<IBassStreamPipelineFactory>();
            if (this.BassStreamPipelineFactory != null)
            {
                this.BassStreamPipelineFactory.CreatingPipeline += this.OnCreatingPipeline;
            }
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<SelectionConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassDsdBehaviourConfiguration.DSD_RATE_ELEMENT
            ).ConnectValue(value => this.Rate = BassDsdBehaviourConfiguration.GetRate(value));
            this.Configuration.GetElement<IntegerConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassDsdBehaviourConfiguration.DSD_GAIN_ELEMENT
            ).ConnectValue(value => this.Gain = value);
            this.Configuration.GetElement<BooleanConfigurationElement>(
                BassOutputConfiguration.SECTION,
                BassDsdBehaviourConfiguration.DSD_MEMORY_ELEMENT
            ).ConnectValue(value => this.Memory = value);
            base.InitializeComponent(core);
        }

        protected virtual void OnCreatingPipeline(object sender, CreatingPipelineEventArgs e)
        {
            if (!BassDsdMemoryStreamComponent.ShouldCreate(this, e.Stream, e.Query))
            {
                return;
            }
            var component = new BassDsdMemoryStreamComponent(this, e.Pipeline, e.Stream.Flags);
            component.InitializeComponent(this.Core);
            e.Components.Add(component);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return BassDsdBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
