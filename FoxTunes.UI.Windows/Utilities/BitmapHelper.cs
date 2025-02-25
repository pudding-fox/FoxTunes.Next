using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoxTunes
{
    public static class BitmapHelper
    {
        static BitmapHelper()
        {
            Loader.Load("bitmap_utilities.dll");
        }

        public const int COLOR_FROM_X = 1;
        public const int COLOR_FROM_Y = 2;
        public const int ALPHA_BLENDING = 4;

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "create_palette")]
        public static extern IntPtr CreatePalette([In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)] Int32Color[] colors, int count, int flags);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "destroy_palette")]
        public static extern bool DestroyPalette(ref IntPtr palette);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "draw_rectangles")]
        public static extern bool DrawRectangles([In] ref RenderInfo info, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Int32Rect[] rectangles, int count);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "draw_rectangles")]
        public static extern bool DrawRectangles([In] ref RenderInfo info, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Int32Rect[,] rectangles, int count);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "draw_rectangle")]
        public static extern bool DrawRectangle([In] ref RenderInfo info, int x, int y, int width, int height);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "draw_lines")]
        public static extern bool DrawLines([In] ref RenderInfo info, [In, MarshalAs(UnmanagedType.LPArray)] Int32Point[,] points, int dimentions, int count);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "draw_line")]
        public static extern bool DrawLine([In] ref RenderInfo info, int x1, int y1, int x2, int y2);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "draw_pixels")]
        public static extern bool DrawDots([In] ref RenderInfo info, [In, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] Int32Pixel[] pixels, int count);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "draw_pixel")]
        public static extern bool DrawDot([In] ref RenderInfo info, int color, int x, int y);

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "shift_left")]
        public static extern bool ShiftLeft([In] ref RenderInfo info, int count);

        public static bool Clear(ref RenderInfo info, Color color)
        {
            var temp = new Int32Color(color);
            return Clear(ref info, ref temp);
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "clear")]
        public static extern bool Clear([In] ref RenderInfo info, [In] ref Int32Color color);

        public static bool Clear(ref RenderInfo info)
        {
            return Clear(ref info, IntPtr.Zero);
        }

        [SuppressUnmanagedCodeSecurity]
        [DllImport("bitmap_utilities.dll", EntryPoint = "clear")]
        private static extern bool Clear([In] ref RenderInfo info, [In] IntPtr color);

        public static RenderInfo CreateRenderInfo(WriteableBitmap bitmap, IntPtr palette)
        {
            if (bitmap.Format != PixelFormats.Pbgra32)
            {
                throw new NotImplementedException();
            }

            return new RenderInfo
            {
                BytesPerPixel = bitmap.Format.BitsPerPixel / 8,
                Width = bitmap.PixelWidth,
                Height = bitmap.PixelHeight,
                Stride = bitmap.PixelWidth * (bitmap.Format.BitsPerPixel / 8),
                Buffer = bitmap.BackBuffer,
                Palette = palette
            };
        }

        public static RenderInfo CreateRenderInfo(RenderInfo renderInfo, IntPtr palette)
        {
            return new RenderInfo
            {
                BytesPerPixel = renderInfo.BytesPerPixel,
                Width = renderInfo.Width,
                Height = renderInfo.Height,
                Stride = renderInfo.Stride,
                Buffer = renderInfo.Buffer,
                Palette = palette
            };
        }

        public static IntPtr CreatePalette(int flags, bool alphaBlending, params Color[] colors)
        {
            return CreatePalette(flags, alphaBlending, colors.Select(color => new Int32Color(color)).ToArray());
        }

        public static IntPtr CreatePalette(int flags, bool alphaBlending, params Int32Color[] colors)
        {
            if (alphaBlending)
            {
                flags |= ALPHA_BLENDING;
            }
            return CreatePalette(colors, colors.Length, flags);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RenderInfo
        {
            public int BytesPerPixel;

            public int Width;

            public int Height;

            public int Stride;

            public IntPtr Buffer;

            public IntPtr Palette;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int32Color
    {
        public Int32Color(Color color) : this(color.B, color.G, color.R, color.A)
        {

        }

        public Int32Color(byte blue, byte green, byte red, byte alpha)
        {
            this.Blue = blue;
            this.Green = green;
            this.Red = red;
            this.Alpha = alpha;
        }

        public byte Blue;

        public byte Green;

        public byte Red;

        public byte Alpha;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int32Point
    {
        public Int32Point(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public int X;

        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int32Pixel
    {
        public Int32Pixel(int x, int y, int color)
        {
            this.X = x;
            this.Y = y;
            this.Color = color;
        }

        public int X;

        public int Y;

        public int Color;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Int32Rect
    {
        public Int32Rect(int x, int y, int width, int height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        public int X;

        public int Y;

        public int Width;

        public int Height;
    }
}
