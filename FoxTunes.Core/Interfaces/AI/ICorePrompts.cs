namespace FoxTunes.Interfaces
{
    public interface ICorePrompts : IBaseComponent
    {
        string AvailableTracks { get; }

        string CreatePlaylist(string prompt);
    }
}
