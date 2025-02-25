using System.Collections.Generic;

namespace FoxTunes
{
    public static partial class Extensions
    {
        public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> sequence)
        {
            foreach (var element in sequence)
            {
                set.Add(element);
            }
        }
    }
}
