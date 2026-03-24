using FoxTunes.Interfaces;
using OpenAI;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    [Component(ID, ComponentSlots.AIRuntime)]
    [ComponentPreference(ComponentPreferenceAttribute.DEFAULT)]
    public class OpenAIRuntime : AIRuntime, IConfigurableComponent, IDisposable
    {
        const string ID = "782E9988-1260-48E1-BF1A-0995E34A9263";

        public static string VERSION = typeof(OpenAIClient).Assembly.GetName().Version.ToString();

        public OpenAIRuntime() : base(ID, string.Format(Strings.OpenAIRuntime_Name, VERSION))
        {
            this.Client = new Lazy<OpenAIClient>(this.CreateClient);
        }

        public Lazy<OpenAIClient> Client { get; private set; }

        public ICore Core { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public TextConfigurationElement ApiKey { get; private set; }

        public TextConfigurationElement Model { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Core = core;
            this.Configuration = core.Components.Configuration;
            this.ApiKey = this.Configuration.GetElement<TextConfigurationElement>(
                OpenAIRuntimeConfiguration.SECTION,
                OpenAIRuntimeConfiguration.API_KEY
            );
            this.Model = this.Configuration.GetElement<TextConfigurationElement>(
                OpenAIRuntimeConfiguration.SECTION,
                OpenAIRuntimeConfiguration.MODEL
            );
            base.InitializeComponent(core);
        }

        protected virtual OpenAIClient CreateClient()
        {
            if (string.IsNullOrEmpty(this.ApiKey.Value))
            {
                Logger.Write(this, LogLevel.Warn, "Cannot create OpenAIClient, no API key.");
                throw new InvalidOperationException(Strings.OpenAIRuntime_NoApiKey);
            }
            Logger.Write(this, LogLevel.Debug, "Creating OpenAIClient.");
            return new OpenAIClient(this.ApiKey.Value);
        }

        public override IAIContext CreateContext()
        {
            var model = this.Model.Value;
            if (string.IsNullOrEmpty(model))
            {
                model = OpenAIRuntimeConfiguration.DEFAULT_MODEL;
            }
            var client = this.Client.Value;
            return new OpenAIContext(client, model);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return OpenAIRuntimeConfiguration.GetConfigurationSections();
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
            //Nothing to do.
        }

        ~OpenAIRuntime()
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
