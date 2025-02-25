using System.Windows.Interop;

namespace FoxTunes
{
    public static class DpiHelper
    {
        public static void TransformPosition(ref int x, ref int y)
        {
            using (var source = new HwndSource(new HwndSourceParameters()))
            {
                if (source.CompositionTarget.TransformToDevice.M11 != 1)
                {
                    x = (int)(x / source.CompositionTarget.TransformToDevice.M11);
                }
                if (source.CompositionTarget.TransformToDevice.M22 != 1)
                {
                    y = (int)(y / source.CompositionTarget.TransformToDevice.M11);
                }
            }
        }
    }
}
