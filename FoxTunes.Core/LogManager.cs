using FoxTunes.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    public static class LogManager
    {
        public static string FileName = Path.Combine(
            Publication.StoragePath,
            "Log.txt"
        );

        public static void Open()
        {
            try
            {
                var fileName = Path.Combine(
                    Path.GetTempPath(),
                    string.Format("Log-{0}.txt", DateTime.Now.ToFileTimeUtc())
                );
                File.Copy(FileName, fileName, true);
                Process.Start(fileName).WaitForExit();
                File.Delete(fileName);
            }
            catch
            {
                //Nothing can be done. If we can't see the log there's no point writing to it....
            }
        }

        private static ILogger _Logger { get; set; }

        public static ILogger Logger
        {
            get
            {
                if (_Logger == null)
                {
                    return NullLogger.Instance;
                }
                return _Logger;
            }
            set
            {
                _Logger = value;
            }
        }

        private class NullLogger : BaseComponent, ILogger
        {
            public bool IsTraceEnabled(IBaseComponent component)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public bool IsDebugEnabled(IBaseComponent component)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public bool IsInfoEnabled(IBaseComponent component)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public bool IsWarnEnabled(IBaseComponent component)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public bool IsErrorEnabled(IBaseComponent component)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public bool IsFatalEnabled(IBaseComponent component)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public bool IsTraceEnabled(Type type)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public bool IsDebugEnabled(Type type)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public bool IsInfoEnabled(Type type)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public bool IsWarnEnabled(Type type)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public bool IsErrorEnabled(Type type)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public bool IsFatalEnabled(Type type)
            {
#if DEBUG
                return true;
#else
                return false;
#endif
            }

            public void Write(IBaseComponent component, LogLevel level, string message, params object[] args)
            {
#if DEBUG
                global::System.Diagnostics.Debug.WriteLine(this.FormatMessage(component.GetType(), level, message, args));
#endif
            }

            public void Write(Type type, LogLevel level, string message, params object[] args)
            {
#if DEBUG
                global::System.Diagnostics.Debug.WriteLine(this.FormatMessage(type, level, message, args));
#endif
            }

            public Task WriteAsync(IBaseComponent component, LogLevel level, string message, params object[] args)
            {
#if DEBUG
                global::System.Diagnostics.Debug.WriteLine(this.FormatMessage(component.GetType(), level, message, args));
#endif
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }

            public Task WriteAsync(Type type, LogLevel level, string message, params object[] args)
            {
#if DEBUG
                global::System.Diagnostics.Debug.WriteLine(this.FormatMessage(type, level, message, args));
#endif
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
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

            public void Dispose()
            {
                //Nothing to do.
            }

            public static readonly ILogger Instance = new NullLogger();
        }
    }
}
