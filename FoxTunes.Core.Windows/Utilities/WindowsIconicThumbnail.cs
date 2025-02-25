using System;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    public static class WindowsIconicThumbnail
    {
        public const int WM_DWMSENDICONICTHUMBNAIL = 0x0323;

        public const int WM_DWMSENDICONICLIVEPREVIEWBITMAP = 0x0326;

        public const int DWM_FORCE_ICONIC_REPRESENTATION = 0x7;

        public const int DWM_HAS_ICONIC_BITMAP = 0xA;

        public const int DWM_DISPLAYTHUMBNAILFRAME = 0x1;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern HResult DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern HResult DwmSetIconicThumbnail(IntPtr hwnd, IntPtr hBitmap, int flags);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern HResult DwmSetIconicLivePreviewBitmap(IntPtr hwnd, IntPtr hBitmap, ref POINT ptClient, int flags);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        public static extern HResult DwmInvalidateIconicBitmaps(IntPtr hwnd);

        public enum HResult
        {
            Ok = 0x0000
        }

        [StructLayout(LayoutKind.Sequential)]
        public class POINT
        {
            public int X;
            public int Y;
        }
    }
}
