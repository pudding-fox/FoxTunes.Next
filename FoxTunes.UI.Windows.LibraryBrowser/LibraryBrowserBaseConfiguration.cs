using System.Collections.Generic;

namespace FoxTunes
{
    public static class LibraryBrowserBaseConfiguration
    {
        public const string SECTION = "DCC06D78-3029-49F9-A05C-C419F73F2E8B";

        public const string TILE_SIZE = "AAAA0E5E-FB67-43D3-AE30-BF7571A1A8B1";

        public const int MIN_TILE_SIZE = 60;

        public const int MAX_TILE_SIZE = 300;

        public const int DEFAULT_TILE_SIZE = 160;

        public const int TILE_SIZE_SMALL = 100;

        public const int TILE_SIZE_MEDIUM = 160;

        public const int TILE_SIZE_LARGE = 300;

        public const string TILE_IMAGE = "BBBBFCBE-2AA0-4B99-9C6E-E3F196540807";

        public const string TILE_IMAGE_FIRST = "AAAA9419-8D7E-4150-8FB8-999D2C019941";

        public const string TILE_IMAGE_COMPOUND = "BBBB32EB-B876-4F72-BA33-88D84817EE30";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.LibraryBrowserBaseConfiguration_Section)
                .WithElement(
                    new SelectionConfigurationElement(TILE_IMAGE, Strings.LibraryBrowserBaseConfiguration_Image)
                        .WithOptions(GetLibraryImageOptions()))
                .WithElement(
                    new IntegerConfigurationElement(TILE_SIZE, Strings.LibraryBrowserBaseConfiguration_Size)
                        .WithValue(DEFAULT_TILE_SIZE)
                        .WithValidationRule(new IntegerValidationRule(MIN_TILE_SIZE, MAX_TILE_SIZE, 4))
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetLibraryImageOptions()
        {
            yield return new SelectionConfigurationOption(TILE_IMAGE_FIRST, Strings.LibraryBrowserBaseConfiguration_Image_First);
            yield return new SelectionConfigurationOption(TILE_IMAGE_COMPOUND, Strings.LibraryBrowserBaseConfiguration_Image_Compound).Default();
        }

        public static LibraryBrowserImageMode GetLibraryImage(SelectionConfigurationOption option)
        {
            switch (option.Id)
            {
                case TILE_IMAGE_FIRST:
                    return LibraryBrowserImageMode.First;
                default:
                case TILE_IMAGE_COMPOUND:
                    return LibraryBrowserImageMode.Compound;
            }
        }
    }

    public enum LibraryBrowserImageMode : byte
    {
        None,
        First,
        Compound
    }
}
