using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IConfigurableComponent : IBaseComponent
    {
        IEnumerable<ConfigurationSection> GetConfigurationSections();
    }
}
