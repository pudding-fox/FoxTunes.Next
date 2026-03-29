using FoxTunes.Interfaces;
using System;
using System.Threading;

namespace FoxTunes
{
    public class CancellationToken : ICancellable
    {
        public bool IsCancellationRequested { get; private set; }

        protected virtual void OnCancellationRequested()
        {
            if (this.CancellationRequested == null)
            {
                return;
            }
            this.CancellationRequested(this, EventArgs.Empty);
        }

        public event EventHandler CancellationRequested;

        public void Cancel()
        {
            this.IsCancellationRequested = true;
            this.OnCancellationRequested();
        }

        public void Reset()
        {
            this.IsCancellationRequested = false;
        }

        public global::System.Threading.CancellationToken ToNative()
        {
            var source = new CancellationTokenSource();
            var handler = new EventHandler((sender, e) => source.Cancel());
            this.CancellationRequested += handler;
            GCTracker.AddCallBack(this, () => this.CancellationRequested -= handler);
            return source.Token;
        }

        public static CancellationToken None
        {
            get
            {
                return new CancellationToken();
            }
        }
    }
}
