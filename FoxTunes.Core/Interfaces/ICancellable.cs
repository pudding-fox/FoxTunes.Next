using System;

namespace FoxTunes.Interfaces
{
    public interface ICancellable
    {
        bool IsCancellationRequested { get; }

        event EventHandler CancellationRequested;
    }
}
