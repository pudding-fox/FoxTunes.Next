namespace FoxTunes.Interfaces
{
    public interface IFileAssociation
    {
        string Extension { get; }

        string ProgId { get; }

        string ExecutableFilePath { get; }
    }
}
