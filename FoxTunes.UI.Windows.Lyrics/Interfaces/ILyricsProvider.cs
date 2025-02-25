using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface ILyricsProvider : IStandardComponent
    {
        string Id { get; }

        string Name { get; }

        string Description { get; }

        //TODO: This is actually a placeholder value written to the database to indicate that a previous lookup has failed.B
        string None { get; }

        Task<LyricsResult> Lookup(IFileData fileData);
    }
}
