using System;

namespace FoxTunes
{
    public class WeakReference<T> : WeakReference where T : class
    {
        public WeakReference(T target) : base(target)
        {

        }

        new public T Target
        {
            get
            {
                return base.Target as T;
            }
        }
    }
}
