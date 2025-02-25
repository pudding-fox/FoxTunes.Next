using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class ImageLoaderConfiguration
    {
        public const string HIGH_QUALITY_RESIZER = "AAAA03BB-6761-4371-B0DF-8D59191FA489";

        public const string CACHE_SIZE = "BBBBDADA-446D-490B-BDDC-446CCC063AD5";

        public const string THREADS = "CCCC8536-6631-4B67-A84A-00AF6218E7BA";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            var cacheSize = default(int);
            switch (Publication.ReleaseType)
            {
                case ReleaseType.Minimal:
                    cacheSize = 128;
                    break;
                default:
                    cacheSize = 1024;
                    break;
            }
            yield return new ConfigurationSection(ImageBehaviourConfiguration.SECTION, "Images")
                .WithElement(new BooleanConfigurationElement(HIGH_QUALITY_RESIZER, "High Quality Resizer").WithValue(true))
                .WithElement(new IntegerConfigurationElement(CACHE_SIZE, "Cache Size", path: "Advanced").WithValue(cacheSize).WithValidationRule(new IntegerValidationRule(64, 2048)))
                .WithElement(new IntegerConfigurationElement(THREADS, "Background Threads", path: "Advanced").WithValue(Environment.ProcessorCount).WithValidationRule(new IntegerValidationRule(1, 32))
            );
        }
    }
}
