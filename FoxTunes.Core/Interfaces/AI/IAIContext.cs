using System;

namespace FoxTunes.Interfaces
{
    public interface IAIContext : IDisposable
    {
        string Model { get; }

        IAIFileStore CreateFileStore();

        IAIVectorStore CreateVectorStore();

        IAIResponseStore CreateResponseStore();
    }
}
