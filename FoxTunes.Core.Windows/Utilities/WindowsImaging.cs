using FoxTunes.Interfaces;
using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    public static class WindowsImaging
    {
        public const int DIB_RGB_COLORS = 0x0;

        public const int ILC_COLOR32 = 0x00000020;

        public static Bitmap Resize(Bitmap sourceImage, int width, int height, bool scale)
        {
            var rectangle = default(Rectangle);
            if (scale)
            {
                Scale(sourceImage.Width, sourceImage.Height, width, height, out rectangle);
            }
            else
            {
                rectangle = new Rectangle(0, 0, width, height);
            }
            var resultImage = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(resultImage))
            {
                graphics.InterpolationMode = InterpolationMode.High;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.DrawImage(sourceImage, rectangle);
            }
            return resultImage;
        }

        public static void Scale(int sourceWidth, int sourceHeight, int targetWidth, int targetHeight, out Rectangle rectangle)
        {
            var ratioX = Convert.ToSingle(targetWidth) / sourceWidth;
            var ratioY = Convert.ToSingle(targetHeight) / sourceHeight;
            var ratio = Math.Min(ratioX, ratioY);
            var actualWidth = Convert.ToInt32(sourceWidth * ratio);
            var actualHeight = Convert.ToInt32(sourceHeight * ratio);
            var x = (targetWidth / 2) - (actualWidth / 2);
            var y = (targetHeight / 2) - (actualHeight / 2);
            rectangle = new Rectangle(x, y, actualWidth, actualHeight);
        }

        public static bool CreateDIBSection(IntPtr hdc, Bitmap bitmap, int width, int height, out IntPtr bitmapSection)
        {
            var bitmapBits = default(IntPtr);
            var bitmapInfo = new BITMAPINFO()
            {
                biSize = Convert.ToUInt32(Marshal.SizeOf(typeof(BITMAPINFO))),
                biBitCount = 32,
                biPlanes = 1,
                biWidth = width,
                biHeight = height
            };
            bitmapSection = CreateDIBSection(
                hdc,
                ref bitmapInfo,
                DIB_RGB_COLORS,
                out bitmapBits,
                IntPtr.Zero,
                0
            );
            var result = default(bool);
            if (!IntPtr.Zero.Equals(bitmapSection))
            {
                var bitmapData = bitmap.LockBits(
                    new Rectangle(
                        0,
                        0,
                        bitmap.Width,
                        bitmap.Height
                    ),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb
                );
                result = RtlMoveMemory(
                    bitmapBits,
                    bitmapData.Scan0,
                    bitmap.Height * bitmapData.Stride
                );
                bitmap.UnlockBits(bitmapData);
            }
            return result;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        public static extern bool ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateDIBSection(IntPtr hdc, [In] ref BITMAPINFO pbmi, uint iUsage, out IntPtr ppvBits, IntPtr hSection, uint dwOffset);

        [DllImport("kernel32.dll")]
        public static extern bool RtlMoveMemory(IntPtr dest, IntPtr source, int dwcount);

        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject([In] IntPtr hObject);

        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFO
        {
            public UInt32 biSize;
            public Int32 biWidth;
            public Int32 biHeight;
            public UInt16 biPlanes;
            public UInt16 biBitCount;
            public Int32 biCompression;
            public UInt32 biSizeImage;
            public Int32 biXPelsPerMeter;
            public Int32 biYPelsPerMeter;
            public UInt32 biClrUsed;
            public UInt32 biClrImportant;
        };

        public class ScopedDC : IDisposable
        {
            protected static ILogger Logger
            {
                get
                {
                    return LogManager.Logger;
                }
            }

            public ScopedDC(IntPtr hwnd, IntPtr dc, IntPtr cdc)
            {
                this.hwnd = hwnd;
                this.dc = dc;
                this.cdc = cdc;
            }

            public IntPtr hwnd { get; private set; }

            public IntPtr dc { get; private set; }

            public IntPtr cdc { get; private set; }

            public bool IsValid
            {
                get
                {
                    return !IntPtr.Zero.Equals(this.dc) && !IntPtr.Zero.Equals(this.cdc);
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
                if (!IntPtr.Zero.Equals(this.cdc))
                {
                    DeleteDC(this.cdc);
                }
                if (!IntPtr.Zero.Equals(this.dc))
                {
                    ReleaseDC(this.hwnd, this.dc);
                }
            }

            ~ScopedDC()
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

            public static ScopedDC Compatible()
            {
                return Compatible(IntPtr.Zero);
            }

            public static ScopedDC Compatible(IntPtr hwnd)
            {
                var dc = GetDC(hwnd);
                var cdc = CreateCompatibleDC(dc);
                return new ScopedDC(hwnd, dc, cdc);
            }
        }
    }
}
