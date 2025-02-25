namespace FoxTunes.Interfaces
{
    public interface IBassOutputStream : IOutputStream
    {
        IBassStream Stream { get; }
    }
}
