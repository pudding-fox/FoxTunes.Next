using System;
using System.Collections.Concurrent;
using System.Windows;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class PixelSizeConverter : StandardComponent
    {
        public PixelSizeConverter()
        {
            this.Store = new Cache();
        }

        public Cache Store { get; private set; }

        public void Convert(ref Size size)
        {
            size = this.Convert(size);
        }

        public Size Convert(Size size)
        {
            return this.Store.GetOrAdd(size, () => Windows.ActiveWindow.GetElementPixelSize(size));
        }

        public void Convert(ref int width, ref int height)
        {
            var size = this.Convert(new Size(width, height));
            width = global::System.Convert.ToInt32(size.Width);
            height = global::System.Convert.ToInt32(size.Height);
        }

        public class Cache
        {
            public Cache()
            {
                this.Store = new ConcurrentDictionary<Size, Size>();
            }

            public ConcurrentDictionary<Size, Size> Store { get; private set; }

            public Size GetOrAdd(Size size, Func<Size> factory)
            {
                return this.Store.GetOrAdd(size, factory);
            }

            public void Clear()
            {
                this.Store.Clear();
            }
        }
    }
}