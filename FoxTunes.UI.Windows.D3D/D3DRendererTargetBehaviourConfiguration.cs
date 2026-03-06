using System.Collections.Generic;

namespace FoxTunes
{
    public static class D3DRendererTargetBehaviourConfiguration
    {
        public const string SECTION = RendererTargetFactoryConfiguration.SECTION;

        public const string BACKEND = RendererTargetFactoryConfiguration.BACKEND;

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new SelectionConfigurationElement(BACKEND).WithOptions(GetBackends())
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetBackends()
        {
            yield return new SelectionConfigurationOption(D3DRendererTargetBehaviour.ID, Strings.D3DRendererTargetBehaviour_Name);
        }
    }
}
