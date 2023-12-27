using ManagedBass;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IBassStreamAdvice : IBaseComponent
    {
        string FileName { get; }

        bool Wrap(IBassStreamProvider provider, int channelHandle, IEnumerable<IBassStreamAdvice> advice, BassFlags flags, out IBassStream stream);
    }
}
