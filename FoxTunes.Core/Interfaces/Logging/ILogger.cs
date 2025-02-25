using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface ILogger : IBaseComponent
    {
        bool IsTraceEnabled(IBaseComponent component);

        bool IsDebugEnabled(IBaseComponent component);

        bool IsInfoEnabled(IBaseComponent component);

        bool IsWarnEnabled(IBaseComponent component);

        bool IsErrorEnabled(IBaseComponent component);

        bool IsFatalEnabled(IBaseComponent component);

        bool IsTraceEnabled(Type type);

        bool IsDebugEnabled(Type type);

        bool IsInfoEnabled(Type type);

        bool IsWarnEnabled(Type type);

        bool IsErrorEnabled(Type type);

        bool IsFatalEnabled(Type type);

        void Write(IBaseComponent component, LogLevel level, string message, params object[] args);

        void Write(Type type, LogLevel level, string message, params object[] args);

        Task WriteAsync(IBaseComponent component, LogLevel level, string message, params object[] args);

        Task WriteAsync(Type type, LogLevel level, string message, params object[] args);
    }

    [Flags]
    public enum LogLevel : byte
    {
        None = 0,
        Trace = 1 | Debug,
        Debug = 2 | Info,
        Info = 4 | Warn,
        Warn = 8 | Error,
        Error = 16 | Fatal,
        Fatal = 32
    }
}
