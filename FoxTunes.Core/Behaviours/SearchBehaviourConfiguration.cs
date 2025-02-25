using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class SearchBehaviourConfiguration
    {
        public const string SECTION = "99797338-186F-4F4E-A6A1-C39546D2C047";

        public const string SEARCH_INTERVAL_ELEMENT = "AAAA9662-3EA7-427E-86A4-74ADFE44F097";

        public const string SEARCH_COMMIT_ELEMENT = "BBBBA37-0407-4EA5-B44E-C0AE5E267B41";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.SearchBehaviourConfiguration_Section)
                .WithElement(
                    new IntegerConfigurationElement(SEARCH_INTERVAL_ELEMENT, Strings.SearchBehaviourConfiguration_Interval).WithValue(1000).WithValidationRule(new IntegerValidationRule(100, 1000, 100)))
                .WithElement(
                    new SelectionConfigurationElement(SEARCH_COMMIT_ELEMENT, Strings.SearchBehaviourConfiguration_Commit).WithOptions(GetCommitBehaviourOptions())
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetCommitBehaviourOptions()
        {
            foreach (var behaviour in new[] { SearchCommitBehaviour.Replace, SearchCommitBehaviour.Append })
            {
                yield return new SelectionConfigurationOption(
                    Enum.GetName(typeof(SearchCommitBehaviour), behaviour),
                    Enum.GetName(typeof(SearchCommitBehaviour), behaviour)
                );
            }
        }

        public static SearchCommitBehaviour GetCommitBehaviour(SelectionConfigurationOption option)
        {
            var result = default(SearchCommitBehaviour);
            if (Enum.TryParse<SearchCommitBehaviour>(option.Id, out result))
            {
                return result;
            }
            return SearchCommitBehaviour.Replace;
        }
    }

    public enum SearchCommitBehaviour : byte
    {
        None = 0,
        Append = 1,
        Replace = 2
    }
}
