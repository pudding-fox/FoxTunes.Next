using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    public class AIBehaviour : StandardComponent, IConfigurableComponent
    {
        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return AIBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
