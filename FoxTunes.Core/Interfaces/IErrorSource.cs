using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IErrorSource : IStandardComponent
    {
        event ComponentErrorEventHandler Error;
    }

    public delegate Task ComponentErrorEventHandler(object sender, ComponentErrorEventArgs e);

    public class ComponentErrorEventArgs : EventArgs
    {
        public ComponentErrorEventArgs(IBaseComponent source, Exception exception) : this(source, exception.Message, exception)
        {
        }

        public ComponentErrorEventArgs(IBaseComponent source, string message, Exception exception)
        {
            this.Source = source;
            this.Message = message;
            this.Exception = exception;
        }

        public IBaseComponent Source { get; private set; }

        public string Message { get; private set; }

        public Exception Exception { get; private set; }
    }
}
