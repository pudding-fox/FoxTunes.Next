using System;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class DelayedCallback : BaseComponent
    {
        public DelayedCallback(Action action, TimeSpan delay)
        {
            this.Action = action;
            this.Delay = delay;
        }

        public Action Action { get; private set; }

        public TimeSpan Delay { get; private set; }

        public bool Enabled { get; private set; }

        public void Enable()
        {
            if (this.Enabled)
            {
                return;
            }
            this.Enabled = true;
            this.Dispatch(async () =>
            {
                //TODO: Double check locking without synchronization.
                if (!this.Enabled)
                {
                    return;
                }
#if NET40
                await TaskEx.Delay(this.Delay).ConfigureAwait(false);
#else
                await Task.Delay(this.Delay).ConfigureAwait(false);
#endif
                if (!this.Enabled)
                {
                    return;
                }
                this.Action();
                this.Enabled = false;
            });
        }

        public void Disable()
        {
            this.Enabled = false;
        }
    }
}
