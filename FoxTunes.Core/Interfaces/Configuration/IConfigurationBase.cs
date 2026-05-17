using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IConfigurationBase : IBaseComponent
    {
        IEnumerable<ConfigurationSection> Sections { get; }

        IConfigurationBase WithSection(ConfigurationSection section);

        void Load();

        event EventHandler Loading;

        event EventHandler Loaded;

        void Save();

        event OrderedEventHandler Saving;

        event EventHandler Saved;

        void Delete();

        void Reset();

        void ConnectDependencies();

        ConfigurationSection GetSection(string sectionId);

        ConfigurationElement GetElement(string sectionId, string elementId);

        T GetElement<T>(string sectionId, string elementId) where T : ConfigurationElement;
    }
}
