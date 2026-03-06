using System;
using System.Windows;
using System.Windows.Media;

namespace FoxTunes
{
    public abstract class RendererTarget : BaseComponent
    {
        public static readonly Duration LockTimeout = new Duration(TimeSpan.FromMilliseconds(1));

        public const double DPIX = 96;

        public const double DPIY = 96;

        public abstract ImageSource ImageSource { get; }

        public abstract int BitsPerPixel { get; }

        public abstract int Width { get; }

        public abstract int Height { get; }

        public abstract IntPtr Buffer { get; }

        public abstract void Invalidate();

        public abstract bool TryLock();

        public abstract void Unlock();

        public abstract void Clear();
    }
}
