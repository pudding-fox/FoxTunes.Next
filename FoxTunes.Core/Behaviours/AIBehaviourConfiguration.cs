using System.Collections.Generic;
using System.Data.SqlClient;

namespace FoxTunes
{
    public static class AIBehaviourConfiguration
    {
        public const string SECTION = "701433D1-5B14-4AAB-A58D-F895A7D5F136";

        public const string ENABLED = "AAAA04DD-2FE5-492B-93FD-5F28D67507B6";

        public const string FILE_ID = "AAAA96B0-5E98-4C76-B284-EC68219DC405";

        public const string VECTOR_STORE_ID = "BBBB72A8-606E-4920-9800-E2A750E97B67";

        public const string PLAYLIST_GENERATION_PROMPT_TEMPLATE = "CCCC07E0-DF40-4C5F-9373-980EADCF6CB2";

        public const string DJ_PROMPT_TEMPLATE = "DDDD65F6-C915-4FA0-A28A-824D2E0331E8";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.AIBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.AIBehaviourConfiguration_Enabled))
                .WithElement(new TextConfigurationElement(FILE_ID, Strings.AIBehaviourConfiguration_FileId)
                    .WithFlags(ConfigurationElementFlags.System)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(VECTOR_STORE_ID, Strings.AIBehaviourConfiguration_VectoreStoreId)
                    .WithFlags(ConfigurationElementFlags.System)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(PLAYLIST_GENERATION_PROMPT_TEMPLATE, Strings.AIBehaviourConfiguration_PlaylistGenerationPromptTemplate)
                    .WithValue(Strings.AIBehaviourConfiguration_DefaultPlaylistGenerationPromptTemplate)
                    .WithFlags(ConfigurationElementFlags.MultiLine)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(DJ_PROMPT_TEMPLATE, Strings.AIBehaviourConfiguration_DJPromptTemplate)
                    .WithValue(Strings.AIBehaviourConfiguration_DefaultDJPromptTemplate)
                    .WithFlags(ConfigurationElementFlags.MultiLine)
                    .DependsOn(SECTION, ENABLED));
        }
    }
}
