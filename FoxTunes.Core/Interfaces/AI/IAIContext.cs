using System;

namespace FoxTunes.Interfaces
{
    public interface IAIContext : IDisposable
    {
        string Model { get; }

        float Temperature { get; }

        ReasoningLevel ReasoningLevel { get; }

        IAIFileStore CreateFileStore();

        IAIVectorStore CreateVectorStore();

        IAIResponseStore CreateResponseStore();
    }

    public enum ReasoningLevel : byte
    {
        None,
        Minimal,
        Low,
        Medium,
        High
    }
}
