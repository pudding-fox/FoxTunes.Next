namespace FoxTunes.Interfaces
{
    public interface IEncoderOutputPath : IBaseComponent
    {
        string GetDirectoryName(IFileData fileData);
    }
}
