using System.Collections.Generic;

namespace FoxTunes
{
    public static class AIBehaviourConfiguration
    {
        public const string SECTION = "701433D1-5B14-4AAB-A58D-F895A7D5F136";

        public const string FILE_ID = "AAAA96B0-5E98-4C76-B284-EC68219DC405";

        public const string VECTOR_STORE_ID = "BBBB72A8-606E-4920-9800-E2A750E97B67";

        public const string PLAYLIST_GENERATION_PROMPT_TEMPLATE = "CCCC07E0-DF40-4C5F-9373-980EADCF6CB2";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.AIBehaviourConfiguration_Section)
                .WithElement(new TextConfigurationElement(FILE_ID, Strings.AIBehaviourConfiguration_FileId))
                .WithElement(new TextConfigurationElement(VECTOR_STORE_ID, Strings.AIBehaviourConfiguration_VectoreStoreId))
                .WithElement(new TextConfigurationElement(PLAYLIST_GENERATION_PROMPT_TEMPLATE, Strings.AIBehaviourConfiguration_PlaylistGenerationPromptTemplate)
                    .WithValue(Strings.AIBehaviourConfiguration_DefaultPlaylistGenerationPromptTemplate)
                    .WithFlags(ConfigurationElementFlags.MultiLine));
        }
    }
}
