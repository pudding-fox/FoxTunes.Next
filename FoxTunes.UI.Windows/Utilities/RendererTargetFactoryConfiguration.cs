using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class RendererTargetFactoryConfiguration
    {
        public const string SECTION = "513404FF-7FEB-46B5-9404-6D90090B3543";

        public const string BACKEND = "AAAAF238-1361-44E9-8D21-608A1CB12362";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.RendererTargetFactoryConfiguration_Section)
                .WithElement(new SelectionConfigurationElement(BACKEND, Strings.RendererTargetFactoryConfiguration_Backend));
        }
    }
}
