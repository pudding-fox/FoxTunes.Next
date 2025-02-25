using System;

namespace FoxTunes.Interfaces
{
    public interface IDatabaseFactory : IStandardFactory
    {
        string Id { get; }

        string Name { get; }

        string Description { get; }

        DatabaseFactoryFlags Flags { get; }

        DatabaseTestResult Test();

        void Initialize();

        IDatabaseComponent Create();
    }

    public enum DatabaseTestResult : byte
    {
        OK = 0,
        Missing = 1,
        Mismatch = 2
    }

    [Flags]
    public enum DatabaseFactoryFlags : byte
    {
        None = 0,
        ConfirmCreate = 1
    }
}
