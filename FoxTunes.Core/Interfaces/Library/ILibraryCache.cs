using System;

namespace FoxTunes.Interfaces
{
    public interface ILibraryCache : IStandardComponent
    {
        bool TryGet(int id, out LibraryItem libraryItem);

        LibraryItem Get(int id, Func<LibraryItem> factory);

        LibraryItem AddOrUpdate(LibraryItem libraryItem);
    }
}
