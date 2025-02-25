using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IPlaylistColumnProvider : IBaseComponent
    {
        string Id { get; }

        string Name { get; }

        string Description { get; }

        bool DependsOn(IEnumerable<string> names);

        string GetValue(PlaylistItem playlistItem);
    }
}
