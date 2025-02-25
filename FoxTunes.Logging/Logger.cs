using FoxTunes.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentPriority(ComponentPriorityAttribute.HIGH)]
    [Component("3DC2C04A-CE99-4416-BC27-068E8AC02F56", ComponentSlots.Logger)]
    public class Logger : StandardComponent, ILogger, IDisposable
    {
        const int TIMEOUT = 1;

        public Logger()
        {
            this.Stream = new Lazy<FileStream>(() =>
            {
                try
                {
                    return File.Open(LogManager.FileName, FileMode.Create, FileAccess.Write, FileShare.Read);
                }
                catch
                {
                    //Nothing can be done.
                    return null;
                }
            });
            this.Writer = new Lazy<TextWriter>(() =>
            {
                var stream = this.Stream.Value;
                if (stream == null)
                {
                    return null;
                }
                return new StreamWriter(stream);
            });
            this.Semaphore = new SemaphoreSlim(1, 1);
        }

        public IConfiguration Configuration { get; private set; }

        public bool Enabled
        {
            get
            {
                return object.ReferenceEquals(LogManager.Logger, this);
            }
            set
            {
                if (value)
                {
                    LogManager.Logger = this;
                }
                else
                {
                    LogManager.Logger = null;
                }
            }
        }

        public LogLevel Level { get; private set; }

        public Lazy<FileStream> Stream { get; private set; }

        public Lazy<TextWriter> Writer { get; private set; }

        public SemaphoreSlim Semaphore { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<BooleanConfigurationElement>(
                LoggingBehaviourConfiguration.SECTION,
                LoggingBehaviourConfiguration.ENABLED_ELEMENT
            ).ConnectValue(value => this.Enabled = value);
            this.Configuration.GetElement<SelectionConfigurationElement>(
                LoggingBehaviourConfiguration.SECTION,
                LoggingBehaviourConfiguration.LEVEL_ELEMENT
            ).ConnectValue(value => this.Level = LoggingBehaviourConfiguration.GetLogLevel(value));
            base.InitializeComponent(core);
        }

        public bool IsTraceEnabled(IBaseComponent component)
        {
            return this.IsTraceEnabled(component.GetType());
        }

        public bool IsDebugEnabled(IBaseComponent component)
        {
            return this.IsDebugEnabled(component.GetType());
        }

        public bool IsInfoEnabled(IBaseComponent component)
        {
            return this.IsInfoEnabled(component.GetType());
        }

        public bool IsWarnEnabled(IBaseComponent component)
        {
            return this.IsWarnEnabled(component.GetType());
        }

        public bool IsErrorEnabled(IBaseComponent component)
        {
            return this.IsErrorEnabled(component.GetType());
        }

        public bool IsFatalEnabled(IBaseComponent component)
        {
            return this.IsFatalEnabled(component.GetType());
        }

        public bool IsTraceEnabled(Type type)
        {
            return this.Enabled && this.Level.HasFlag(LogLevel.Trace);
        }

        public bool IsDebugEnabled(Type type)
        {
            return this.Enabled && this.Level.HasFlag(LogLevel.Debug);
        }

        public bool IsInfoEnabled(Type type)
        {
            return this.Enabled && this.Level.HasFlag(LogLevel.Info);
        }

        public bool IsWarnEnabled(Type type)
        {
            return this.Enabled && this.Level.HasFlag(LogLevel.Warn);
        }

        public bool IsErrorEnabled(Type type)
        {
            return this.Enabled && this.Level.HasFlag(LogLevel.Error);
        }

        public bool IsFatalEnabled(Type type)
        {
            return this.Enabled && this.Level.HasFlag(LogLevel.Fatal);
        }

        public void Write(IBaseComponent component, LogLevel level, string message, params object[] args)
        {
            this.Write(component.GetType(), level, message, args);
        }

        [DebuggerNonUserCode]
        public void Write(Type type, LogLevel level, string message, params object[] args)
        {
            if (!this.Enabled || !this.Level.HasFlag(level))
            {
                return;
            }
            if (this.Writer.Value == null)
            {
                //Failed to open the output file, nothing can be done.
                return;
            }
            message = this.FormatMessage(type, level, message, args);
#if DEBUG
            global::System.Diagnostics.Debug.WriteLine(message);
#endif
            try
            {
                if (!this.Semaphore.Wait(TIMEOUT))
                {
                    //Timed out while entering lock, nothing can be done.
                    return;
                }
                try
                {
                    this.Writer.Value.WriteLine(message);
                    this.Writer.Value.Flush();
                }
                finally
                {
                    this.Semaphore.Release();
                }
            }
            catch
            {
                //Nothing can be done, probably shutting down.
            }
        }

        public Task WriteAsync(IBaseComponent component, LogLevel level, string message, params object[] args)
        {
            return this.WriteAsync(component.GetType(), level, message, args);
        }

        [DebuggerNonUserCode]
        public async Task WriteAsync(Type type, LogLevel level, string message, params object[] args)
        {
            if (!this.Enabled || !this.Level.HasFlag(level))
            {
                return;
            }
            if (this.Writer.Value == null)
            {
                //Failed to open the output file, nothing can be done.
                return;
            }
            try
            {
#if NET40
                if (!this.Semaphore.Wait(TIMEOUT))
#else
                if (!await this.Semaphore.WaitAsync(TIMEOUT).ConfigureAwait(false))
#endif
                {
                    //Timed out while entering lock, nothing can be done.
                    return;
                }
                try
                {
                    await this.Writer.Value.WriteLineAsync(this.FormatMessage(type, level, message, args)).ConfigureAwait(false);
                    await this.Writer.Value.FlushAsync().ConfigureAwait(false);
                }
                finally
                {
                    this.Semaphore.Release();
                }
            }
            catch
            {
                //Nothing can be done, probably shutting down.
            }
        }

        protected virtual string FormatMessage(Type type, LogLevel level, string message, object[] args)
        {
            if (args != null && args.Length > 0)
            {
                message = string.Format(message, args);
            }
            return string.Format(
                "{0} {1} {2} : {3}",
                DateTime.Now.Ticks,
                type.FullName,
                Enum.GetName(typeof(LogLevel), level),
                message
            );
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
            //Don't accept any more messages.
            this.Enabled = false;
            if (this.Writer.IsValueCreated)
            {
                this.Writer.Value.Flush();
            }
            if (this.Stream.IsValueCreated)
            {
                this.Stream.Value.Flush();
                //Close the log file.
                this.Stream.Value.Dispose();
            }
        }

        ~Logger()
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
