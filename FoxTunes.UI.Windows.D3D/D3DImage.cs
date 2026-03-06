using FoxTunes.Interfaces;
using SharpDX.Direct3D9;
using System;
using System.Windows.Interop;

namespace FoxTunes
{
    public class D3DImage : global::System.Windows.Interop.D3DImage, IDisposable
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public D3DImage(Device device, int width, int height)
        {
            this.Surface = Surface.CreateRenderTarget(
                device,
                width,
                height,
                Format.X8R8G8B8,
                MultisampleType.None,
                0,
                true
            );
            this.Lock();
            this.SetBackBuffer(
                D3DResourceType.IDirect3DSurface9,
                this.Surface.NativePointer
            );
            this.Unlock();
        }

        public Surface Surface { get; private set; }

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
            if (this.Surface != null)
            {
                this.Surface.Dispose();
            }
        }

        ~D3DImage()
        {
            Logger.Write(typeof(D3DImage), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
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
