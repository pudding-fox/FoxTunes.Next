using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IPlaylistProvider : IStandardBehaviour
    {
        Func<Playlist, bool> Predicate { get; }

        Task Refresh(Playlist playlist, bool force);
    }
}
