using FoxTunes.Interfaces;
using System;

namespace FoxTunes.Proxies
{
    public class JavascriptContext : IDisposable
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public JavascriptContext(object context, JavascriptContextHandlers handlers)
        {
            this.Context = context;
            this.Handlers = handlers;
        }

        public object Context { get; private set; }

        public JavascriptContextHandlers Handlers { get; private set; }

        public object GetParameter(string name)
        {
            return this.Handlers.GetParameter(this.Context, name);
        }

        public void SetParameter(string name, object value)
        {
            this.Handlers.SetParameter(this.Context, name, value);
        }

        public object Run(string script)
        {
            return this.Handlers.Run(this.Context, script);
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
            if (this.Context is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        ~JavascriptContext()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
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

    public class JavascriptContextHandlers
    {
        public JavascriptContextHandlers(Func<object, string, object> getParameter, Action<object, string, object> setParameter, Func<object, string, object> run)
        {
            this.GetParameter = getParameter;
            this.SetParameter = setParameter;
            this.Run = run;
        }

        public Func<object, string, object> GetParameter { get; private set; }

        public Action<object, string, object> SetParameter { get; private set; }

        public Func<object, string, object> Run { get; private set; }
    }
}
