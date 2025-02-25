using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public static class FilterParserConfiguration
    {
        public const string SECTION = SearchBehaviourConfiguration.SECTION;

        public const string SEARCH_NAMES = "82518DC0-AA38-4EEC-8F50-575DE34B1182";

        public static readonly string[] DefaultSearchNames = new[]
        {
            //Common.
            CommonMetaData.Artist,
            CommonMetaData.Performer,
            CommonMetaData.Album,
            CommonMetaData.Title,
            //Path.
            FileSystemProperties.FileName,
            FileSystemProperties.DirectoryName
        };

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION)
                .WithElement(new TextConfigurationElement(SEARCH_NAMES, Strings.FilterParserConfiguration_Names)
                    .WithValue(string.Join(Environment.NewLine, DefaultSearchNames)).WithFlags(ConfigurationElementFlags.MultiLine));
        }

        public static IEnumerable<string> GetSearchNames(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return DefaultSearchNames;
            }
            return value.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Select(
                element => element.Trim()
            ).ToArray();
        }
    }
}
