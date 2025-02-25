using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    public abstract class BassStreamAdvisor : StandardComponent, IBassStreamAdvisor
    {
        public abstract void Advise(IBassStreamProvider provider, PlaylistItem playlistItem, IList<IBassStreamAdvice> advice, BassStreamUsageType type);
    }
}
