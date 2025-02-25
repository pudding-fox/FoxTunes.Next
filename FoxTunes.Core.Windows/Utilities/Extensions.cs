using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FoxTunes
{
    public static partial class Extensions
    {
        const double DPIX = 96;

        const double DPIY = 96;

        public static Task Invoke(this HwndSource source, Action action)
        {
            if (source.Dispatcher.CheckAccess())
            {
                action();
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
#if NET40
            var taskCompletionSource = new TaskCompletionSource<bool>();
            source.Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    action();
                }
                finally
                {
                    taskCompletionSource.SetResult(false);
                }
            }));
            return taskCompletionSource.Task;
#else
            return source.Dispatcher.BeginInvoke(action).Task;
#endif
        }

        public static Bitmap ToBitmap(this Visual visual)
        {
            if (visual is UIElement element)
            {
                var target = new RenderTargetBitmap(
                    Convert.ToInt32(element.RenderSize.Width),
                    Convert.ToInt32(element.RenderSize.Height),
                    DPIX,
                    DPIY,
                    PixelFormats.Pbgra32
                );
                target.Render(visual);
                var encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(target));
                var stream = new MemoryStream();
                encoder.Save(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return Bitmap.FromStream(stream) as Bitmap;
            }
            throw new NotImplementedException();
        }
    }
}
