using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IPlaybackManager : IStandardManager, IDisposable
    {
        bool IsSupported(string fileName);

        IOutputStream CurrentStream { get; }

        event EventHandler CurrentStreamChanged;

        event EventHandler Ending;

        event EventHandler Ended;

        Task Load(PlaylistItem playlistItem, bool immediate);

        Task Stop();
    }
}
