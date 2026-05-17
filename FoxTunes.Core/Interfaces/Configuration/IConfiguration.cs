using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IConfiguration : IConfigurationBase, IStandardComponent
    {
        IEnumerable<string> AvailableProfiles { get; }

        string Profile { get; }

        bool IsDefaultProfile { get; }

        void Load(string profile);

        void Save(string profile);

        void Delete(string profile);
    }
}
