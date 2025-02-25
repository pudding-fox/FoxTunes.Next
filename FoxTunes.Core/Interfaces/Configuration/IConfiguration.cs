using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IConfiguration : IStandardComponent
    {
        IEnumerable<string> AvailableProfiles { get; }

        string Profile { get; }

        bool IsDefaultProfile { get; }

        IEnumerable<ConfigurationSection> Sections { get; }

        IConfiguration WithSection(ConfigurationSection section);

        void Load();

        void Load(string profile);

        event EventHandler Loading;

        event EventHandler Loaded;

        void Save();

        void Save(string profile);

        event OrderedEventHandler Saving;

        event EventHandler Saved;

        void Delete();

        void Delete(string profile);

        void Reset();

        void ConnectDependencies();

        ConfigurationSection GetSection(string sectionId);

        ConfigurationElement GetElement(string sectionId, string elementId);

        T GetElement<T>(string sectionId, string elementId) where T : ConfigurationElement;
    }
}
