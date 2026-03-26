using System.Collections.Generic;

namespace FoxTunes
{
    public static class AILibraryBehaviourConfiguration
    {
        public const string SECTION = AIBehaviourConfiguration.SECTION;

        public const string ENABLED = AIBehaviourConfiguration.ENABLED;

        public const string UPDATE = "ZZZZ1302-10C3-4097-873E-39CE344FF60A";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new CommandConfigurationElement(UPDATE, Strings.AILibraryBehaviourConfiguration_Update).WithHandler(() =>
                {
                    var behaviour = ComponentRegistry.Instance.GetComponent<AILibraryBehaviour>();
                    var task = behaviour.Refresh();
                })
                .DependsOn(SECTION, ENABLED));
        }
    }
}
