using System.Collections.Generic;

namespace FoxTunes
{
    public static class WriteableBitmapRendererTargetBehaviourConfiguration
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
            yield return new SelectionConfigurationOption(WriteableBitmapRendererTargetBehaviour.ID, Strings.WriteableBitmapRendererTargetBehaviour_Name).Default();
        }
    }
}
