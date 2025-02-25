using System;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    public static class WindowsImageList
    {
        [DllImport("comctl32.dll")]
        public static extern IntPtr ImageList_Create(int width, int height, uint flags, int count, int grow);

        [DllImport("comctl32.dll")]
        public static extern bool ImageList_Destroy(IntPtr handle);

        [DllImport("comctl32.dll")]
        public static extern int ImageList_Add(IntPtr imageHandle, IntPtr hBitmap, IntPtr hMask);

        [DllImport("comctl32.dll")]
        public static extern bool ImageList_Remove(IntPtr imageHandle, int index);
    }
}
