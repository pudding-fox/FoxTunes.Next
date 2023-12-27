using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class ErrorEmitter : StandardComponent, IErrorEmitter
    {
        public Task Send(IBaseComponent source, string message)
        {
            return this.Send(source, new Exception(message));
        }

        public Task Send(IBaseComponent source, Exception exception)
        {
            return this.OnError(this, new ComponentErrorEventArgs(source, exception));
        }

        public Task Send(IBaseComponent source, string message, Exception exception)
        {
            return this.OnError(this, new ComponentErrorEventArgs(source, message, exception));
        }

        protected virtual Task OnError(object sender, ComponentErrorEventArgs e)
        {
            Logger.Write(this, LogLevel.Error, e.Message);
            if (this.Error == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return this.Error(sender, e);
        }

        public event ComponentErrorEventHandler Error;
    }
}
