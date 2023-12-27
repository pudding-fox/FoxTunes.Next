using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IPlaylistColumnManager : IStandardManager
    {
        IEnumerable<IPlaylistColumnProvider> Providers { get; }

        IPlaylistColumnProvider GetProvider(string plugin);
    }
}
