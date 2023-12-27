namespace FoxTunes.Interfaces
{
    public interface ITagLibFileAbstraction : global::TagLib.File.IFileAbstraction
    {
        IFileAbstraction FileAbstraction { get; }
    }
}
