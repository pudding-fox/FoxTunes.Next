using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface ICoreSetup
    {
        IEnumerable<string> Slots { get; }

        bool HasSlot(string slot);
    }
}
