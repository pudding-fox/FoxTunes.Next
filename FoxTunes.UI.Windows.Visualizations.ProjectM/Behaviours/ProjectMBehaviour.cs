using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class ProjectMBehaviour : StandardBehaviour, IConfigurableComponent
    {
        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return ProjectMBehaviourConfiguration.GetConfigurationSections();
        }
    }
}
