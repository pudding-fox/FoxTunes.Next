using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoxTunes
{
    public class WriteableBitmapRendererTarget : RendererTarget
    {
        public WriteableBitmapRendererTarget(int width, int height)
        {
            this.Bitmap = new WriteableBitmap(
                width,
                height,
                DPIX,
                DPIY,
                PixelFormats.Pbgra32,
                null
            );
        }

        public WriteableBitmap Bitmap { get; private set; }

        public override ImageSource ImageSource
        {
            get
            {
                return this.Bitmap;
            }
        }

        public override int BitsPerPixel
        {
            get
            {
                return this.Bitmap.Format.BitsPerPixel;
            }
        }

        public override int Stride
        {
            get
            {
                return this.Bitmap.BackBufferStride;
            }
        }

        public override int Width
        {
            get
            {
                return this.Bitmap.PixelWidth;
            }
        }

        public override int Height
        {
            get
            {
                return this.Bitmap.PixelHeight;
            }
        }

        public override IntPtr Buffer
        {
            get
            {
                return this.Bitmap.BackBuffer;
            }
        }

        public override void Invalidate()
        {
            this.Bitmap.AddDirtyRect(new global::System.Windows.Int32Rect(0, 0, this.Width, this.Height));
        }

        public override bool TryLock()
        {
            return this.Bitmap.TryLock(LockTimeout);
        }

        public override void Unlock()
        {
            this.Bitmap.Unlock();
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
    }
}
