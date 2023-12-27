using FoxTunes.Interfaces;
using System;

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

        public static CancellationToken None
        {
            get
            {
                return new CancellationToken();
            }
        }
    }
}
