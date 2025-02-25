using System.Windows;

namespace FoxTunes.Interfaces
{
    public interface IUIPlaylistColumnProvider : IPlaylistColumnProvider
    {
        DataTemplate CellTemplate { get; }
    }
}
