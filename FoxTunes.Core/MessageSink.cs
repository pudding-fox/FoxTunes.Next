using FoxTunes.Interfaces;
using System;

namespace FoxTunes
{
    public abstract class MessageSink : BaseComponent, IMessageSink
    {
        public abstract IntPtr Handle { get; protected set; }

        protected virtual void OnMouseLeftButtonDown()
        {
            if (this.MouseLeftButtonDown != null)
            {
                this.MouseLeftButtonDown(this, EventArgs.Empty);
            }
        }

        public event EventHandler MouseLeftButtonDown;

        protected virtual void OnMouseLeftButtonUp()
        {
            if (this.MouseLeftButtonUp != null)
            {
                this.MouseLeftButtonUp(this, EventArgs.Empty);
            }
        }

        public event EventHandler MouseLeftButtonUp;

        protected virtual void OnMouseRightButtonDown()
        {
            if (this.MouseRightButtonDown != null)
            {
                this.MouseRightButtonDown(this, EventArgs.Empty);
            }
        }

        public event EventHandler MouseRightButtonDown;

        protected virtual void OnMouseRightButtonUp()
        {
            if (this.MouseRightButtonUp != null)
            {
                this.MouseRightButtonUp(this, EventArgs.Empty);
            }
        }

        public event EventHandler MouseRightButtonUp;

        protected virtual void OnMouseMove()
        {
            if (this.MouseMove != null)
            {
                this.MouseMove(this, EventArgs.Empty);
            }
        }

        public event EventHandler MouseMove;

        protected virtual void OnMouseDoubleClick()
        {
            if (this.MouseDoubleClick != null)
            {
                this.MouseDoubleClick(this, EventArgs.Empty);
            }
        }

        public event EventHandler MouseDoubleClick;

        protected virtual void OnTaskBarCreated()
        {
            if (this.TaskBarCreated != null)
            {
                this.TaskBarCreated(this, EventArgs.Empty);
            }
        }

        public event EventHandler TaskBarCreated;

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
            //Nothing to do.
        }

        ~MessageSink()
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
