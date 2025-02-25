using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;

namespace FoxTunes
{
    public class LoggingBehaviour : StandardBehaviour, IConfigurableComponent
    {
        public static string FILE_NAME = "Log.txt";

        public IConfiguration Configuration { get; private set; }

        public bool Enabled { get; private set; }

        public LogLevel Level { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<BooleanConfigurationElement>(
                LoggingBehaviourConfiguration.SECTION,
                LoggingBehaviourConfiguration.ENABLED_ELEMENT
            ).ConnectValue(value => { this.Enabled = value; this.Refresh(); });
            this.Configuration.GetElement<SelectionConfigurationElement>(
                LoggingBehaviourConfiguration.SECTION,
                LoggingBehaviourConfiguration.LEVEL_ELEMENT
            ).ConnectValue(value => { this.Level = LoggingBehaviourConfiguration.GetLogLevel(value); this.Refresh(); });
            base.InitializeComponent(core);
        }

        public void Refresh()
        {
            if (this.Enabled && this.Level.HasFlag(LogLevel.Trace))
            {
                AppDomain.CurrentDomain.FirstChanceException += this.OnFirstChanceException;
                AppDomain.CurrentDomain.UnhandledException += this.OnUnhandledException;
                Logger.Write(this, LogLevel.Debug, "Tracing enabled.");
            }
            else
            {
                AppDomain.CurrentDomain.FirstChanceException -= this.OnFirstChanceException;
                AppDomain.CurrentDomain.UnhandledException -= this.OnUnhandledException;
                Logger.Write(this, LogLevel.Debug, "Tracing disabled.");
            }
        }

        protected virtual void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            Logger.Write(this, LogLevel.Debug, "Exception (Warn): {0} => {1}", e.Exception.Message, e.Exception.StackTrace);
        }

        protected virtual void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (e.ExceptionObject is Exception exception)
            {
                Logger.Write(this, LogLevel.Debug, "Exception (Fatal): {0} => {1}", exception.Message, exception.StackTrace);
            }
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return LoggingBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
