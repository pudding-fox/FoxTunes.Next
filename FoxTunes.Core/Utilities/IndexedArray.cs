using System.Collections.Generic;

namespace FoxTunes
{
    public class IndexedArray<T> : IndexedCollection<T>
    {
        public IndexedArray(T[] sequence)
        {
            this.InnerArray = sequence;
        }

        public T[] InnerArray { get; private set; }

        public override ICollection<T> InnerCollection
        {
            get
            {
                return this.InnerArray;
            }
        }
    }
}
