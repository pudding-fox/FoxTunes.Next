using System.Collections.Generic;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IFileActionHandler : IBaseComponent
    {
        bool CanHandle(string path, FileActionType type);

        Task Handle(IEnumerable<string> paths, FileActionType type);

        Task Handle(IEnumerable<string> paths, int index, FileActionType type);
    }

    public enum FileActionType : byte
    {
        None = 0,
        Playlist = 1,
        Library = 2
    }
}
