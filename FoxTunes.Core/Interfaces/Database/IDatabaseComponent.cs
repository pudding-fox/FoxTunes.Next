using FoxDb.Interfaces;
using System.Data;

namespace FoxTunes.Interfaces
{
    public interface IDatabaseComponent : IDatabase, IBaseComponent, IInitializable
    {
        string FileName { get; }

        IsolationLevel PreferredIsolationLevel { get; }

        IDatabaseTables Tables { get; }

        IDatabaseQueries Queries { get; }
    }
}
