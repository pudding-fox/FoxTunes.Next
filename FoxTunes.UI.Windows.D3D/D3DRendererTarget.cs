using FoxTunes.Interfaces;
using SharpDX;
using SharpDX.Direct3D9;
using System;
using System.Windows.Media;

namespace FoxTunes
{
    public class D3DRendererTarget : RendererTarget, IDisposable
    {
        public D3DRendererTarget(Device device, int width, int height)
        {
            this.Image = new D3DImage(device, width, height);
        }

        public D3DImage Image { get; private set; }

        public DataRectangle DataRectangle { get; private set; }

        public override ImageSource ImageSource
        {
            get
            {
                return this.Image;
            }
        }

        public override int BitsPerPixel
        {
            get
            {
                return 32;
            }
        }

        public override int Width
        {
            get
            {
                return this.Image.PixelWidth;
            }
        }

        public override int Height
        {
            get
            {
                return this.Image.PixelHeight;
            }
        }

        public override IntPtr Buffer
        {
            get
            {
                return this.DataRectangle.DataPointer;
            }
        }

        public override void Invalidate()
        {
            this.Image.AddDirtyRect(new global::System.Windows.Int32Rect(0, 0, this.Width, this.Height));
        }

        public override bool TryLock()
        {
            this.DataRectangle = this.Image.Surface.LockRectangle(LockFlags.None);
            if (!this.Image.TryLock(LockTimeout))
            {
                this.DataRectangle = default(DataRectangle);
                this.Image.Surface.UnlockRectangle();
                return false;
            }
            return true;
        }

        public override void Unlock()
        {
            this.Image.Unlock();
            this.DataRectangle = default(DataRectangle);
            this.Image.Surface.UnlockRectangle();
        }

        public override void Clear()
        {
            if (!this.TryLock())
            {
                return;
            }
            try
            {
                var info = BitmapHelper.CreateRenderInfo(this, IntPtr.Zero);
                BitmapHelper.Clear(ref info);
                this.Invalidate();
            }
            finally
            {
                this.Unlock();
            }
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
            if (this.Image != null)
            {
                this.Image.Dispose();
            }
        }

        ~D3DRendererTarget()
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
