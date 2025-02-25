using System.Collections.Generic;

namespace FoxTunes
{
    public static class DiscogsBehaviourConfiguration
    {
        public const string SECTION = "D41A188F-119B-4CBF-AF10-522A7AC77CAE";

        public const string ENABLED = "AAAA76ED-AC82-4C9F-8FCE-096E3ABC2A47";

        public const string BASE_URL = "BBBB3214-B1DC-4BAB-9435-311C005998EA";

        public const string CONSUMER_KEY = "CCCC15BB-F99D-4B1A-9433-EDC634C94ABF";

        public const string CONSUMER_SECRET = "DDDD019F-AC8C-4C17-9127-0ADB9FE86FD0";

        public const string MAX_REQUESTS = "EEEE9991-C4EC-4DDA-9315-F658BC461283";

        public const string MIN_CONFIDENCE = "FFFFDFA1-BB70-44A8-AA80-DFDD67235356";

        public const string AUTO_LOOKUP = "FFGG9A7F-90EF-42D8-827F-5638A586B398";

        public const string CONFIRM_LOOKUP = "FGGGDF0F-C720-43C7-B8AC-D47F1DE17F8B";

        public const string WRITE_TAGS = "GGGG4763-D42F-46E2-BB25-E225EF15CE67";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.DiscogsBehaviourConfiguration_Section)
                .WithElement(new BooleanConfigurationElement(ENABLED, Strings.DiscogsBehaviourConfiguration_Enabled)
                    .WithValue(false))
                .WithElement(new TextConfigurationElement(BASE_URL, Strings.DiscogsBehaviourConfiguration_BaseUrl, path: Strings.General_Advanced)
                    .WithValue(Discogs.BASE_URL)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new TextConfigurationElement(CONSUMER_KEY, Strings.DiscogsBehaviourConfiguration_ConsumerKey, path: Strings.General_Advanced)
                    .WithValue(Discogs.KEY)
                    .DependsOn(SECTION, ENABLED)
                    .WithFlags(ConfigurationElementFlags.Secret))
                .WithElement(new TextConfigurationElement(CONSUMER_SECRET, Strings.DiscogsBehaviourConfiguration_ConsumerSecret, path: Strings.General_Advanced)
                    .WithValue(Discogs.SECRET)
                    .DependsOn(SECTION, ENABLED)
                    .WithFlags(ConfigurationElementFlags.Secret))
                .WithElement(new IntegerConfigurationElement(MAX_REQUESTS, Strings.DiscogsBehaviourConfiguration_MaxRequests, path: Strings.General_Advanced)
                    .WithValue(Discogs.MAX_REQUESTS)
                    .WithValidationRule(new IntegerValidationRule(1, 10))
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new DoubleConfigurationElement(MIN_CONFIDENCE, Strings.DiscogsBehaviourConfiguration_MinConfidence, path: Strings.General_Advanced)
                    .WithValue(0.8)
                    .WithValidationRule(new DoubleValidationRule(0, 1, 0.1))
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new BooleanConfigurationElement(AUTO_LOOKUP, Strings.DiscogsBehaviourConfiguration_AutoLookup)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new BooleanConfigurationElement(CONFIRM_LOOKUP, Strings.DiscogsBehaviourConfiguration_ConfirmLookup)
                    .WithValue(true)
                    .DependsOn(SECTION, ENABLED))
                .WithElement(new BooleanConfigurationElement(WRITE_TAGS, Strings.DiscogsBehaviourConfiguration_WriteTags)
                    .WithValue(true)
                    .DependsOn(SECTION, ENABLED));
        }
    }
}
