using System;
using System.Runtime.CompilerServices;

namespace FoxTunes
{
    public class GCTracker : BaseComponent, IDisposable
    {
        public ConditionalWeakTable<object, GCTracker> Store { get; private set; }

        public Action CallBack { get; private set; }

        private GCTracker()
        {
            this.Store = new ConditionalWeakTable<object, GCTracker>();
        }

        public GCTracker(object value, Action callBack) : this()
        {
            this.Store.Add(value, this);
            this.CallBack = callBack;
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
            if (this.CallBack != null)
            {
                this.CallBack();
            }
        }

        ~GCTracker()
        {
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        public static void AddCallBack(object value, Action callBack)
        {
            new GCTracker(value, callBack);
        }
    }
}
