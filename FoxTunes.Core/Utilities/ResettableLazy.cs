using System;

namespace FoxTunes
{
    public class ResettableLazy<T>
    {
        public ResettableLazy(Func<T> factory)
        {
            this.Factory = factory;
            this.Reset();
        }

        public Func<T> Factory { get; private set; }

        public Lazy<T> Lazy { get; private set; }

        public bool IsValueCreated
        {
            get
            {
                if (this.Lazy == null)
                {
                    return false;
                }
                return this.Lazy.IsValueCreated;
            }
        }

        public T Value
        {
            get
            {
                return this.Lazy.Value;
            }
        }

        public void Reset()
        {
            this.Lazy = new Lazy<T>(this.Factory);
        }
    }
}
