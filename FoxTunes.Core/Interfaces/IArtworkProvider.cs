using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IArtworkProvider : IStandardComponent
    {
        string GetFileName(string path, string extension, ArtworkType type);

        string Find(string path, ArtworkType type);

        Task<string> Find(IFileData fileData, ArtworkType type);

        void Reset(string path, ArtworkType type);
    }
}
