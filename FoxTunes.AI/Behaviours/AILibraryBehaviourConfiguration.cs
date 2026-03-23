using System.Collections.Generic;

namespace FoxTunes
{
    public static class AILibraryBehaviourConfiguration
    {
        public const string SECTION = "755D932B-0A8E-44FB-98AB-E2CC9EC711AF";

        public const string FILE_ID = "AAAA96B0-5E98-4C76-B284-EC68219DC405";

        public const string VECTOR_STORE_ID = "BBBB72A8-606E-4920-9800-E2A750E97B67";

        public const string UPDATE = "ZZZZ1302-10C3-4097-873E-39CE344FF60A";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.AILibraryBehaviourConfiguration_Section)
                .WithElement(new TextConfigurationElement(FILE_ID, Strings.AILibraryBehaviourConfiguration_FileId))
                .WithElement(new TextConfigurationElement(VECTOR_STORE_ID, Strings.AILibraryBehaviourConfiguration_VectorStoreId))
                .WithElement(new CommandConfigurationElement(UPDATE, Strings.AILibraryBehaviourConfiguration_Update).WithHandler(() =>
                {
                    var behaviour = ComponentRegistry.Instance.GetComponent<AILibraryBehaviour>();
                    var task = behaviour.Refresh();
                }));
        }
    }
}
