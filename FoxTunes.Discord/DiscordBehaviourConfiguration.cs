using System.Collections.Generic;

namespace FoxTunes
{
    public static class DiscordBehaviourConfiguration
    {
        public const string SECTION = "3202EF4C-7643-417C-A07C-926FDCE279EF";

        public const string ENABLED = "AAAA0FEC-C50C-4296-BE5C-7AC8D94EF9A6";

        public const string BUNNY_API_KEY = "BBBB5343-49D3-4BBE-84F2-8B3C47FF3854";

        public const string DEFAULT_BUNNY_API_KEY = "";

        public const string BUNNY_UPLOAD_URL = "CCCC1AC2-D4D6-4B78-B3AE-932B54F23F75";

        public const string DEFAULT_BUNNY_UPLOAD_URL = "https://storage.bunnycdn.com/ft-thumbs";

        public const string BUNNY_DOWNLOAD_URL = "DDDDB4B5-5E15-484A-AE0A-37B2ECD7B585";

        public const string DEFAULT_BUNNY_DOWNLOAD_URL = "https://ft-thumbs.b-cdn.net";

        public const string STATE_SCRIPT = "EEEE2666-6F4A-45BA-825E-FFE82E73CF87";

        public const string DETAILS_SCRIPT = "FFFF0548-618C-4014-95E3-2D6C623B44A7";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.DiscordBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.DiscordBehaviourConfiguration_Enabled))
                .WithElement(new TextConfigurationElement(BUNNY_API_KEY, Strings.DiscordBehaviourConfiguration_BunnyApiKey)
                    .WithValue(DEFAULT_BUNNY_API_KEY)
                    .WithFlags(ConfigurationElementFlags.Secret)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(BUNNY_UPLOAD_URL, Strings.DiscordBehaviourConfiguration_BunnyUploadUrl)
                    .WithValue(DEFAULT_BUNNY_UPLOAD_URL)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(BUNNY_DOWNLOAD_URL, Strings.DiscordBehaviourConfiguration_BunnyDownloadUrl)
                    .WithValue(DEFAULT_BUNNY_DOWNLOAD_URL)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(STATE_SCRIPT, Strings.DiscordBehaviourConfiguration_StateScript, path: Strings.General_Advanced)
                    .WithValue(Resources.State)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(DETAILS_SCRIPT, Strings.DiscordBehaviourConfiguration_DetailsScript, path: Strings.General_Advanced)
                    .WithValue(Resources.Details)
                    .DependsOn(SECTION, ENABLED)
            );
        }
    }
}
