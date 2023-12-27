namespace FoxTunes.Interfaces
{
    public interface IComponentResolver
    {
        bool Enabled { get; }

        bool Get(string slot, out string id);

        void Add(string slot, string id);

        void Remove(string slot);

        void AddConflict(string slot);

        void Save();
    }
}
