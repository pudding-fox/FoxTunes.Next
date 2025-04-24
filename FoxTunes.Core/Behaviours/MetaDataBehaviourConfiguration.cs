using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    public static class MetaDataBehaviourConfiguration
    {
        public const string SECTION = "C1EBA337-A333-444D-9AAE-D8CE36C39605";

        public const string READ_EMBEDDED_IMAGES = "BBBBF119-ABA3-43A7-BEDD-E0640B9A2BC4";

        public const string READ_LOOSE_IMAGES = "CCCCB0DB-9D21-4066-96E4-E677E5DDE2FA";

        public const string IMAGES_PREFERENCE = "CCDD1A78-01E3-4262-B845-91E090E51180";

        public const string LOOSE_IMAGES_FRONT = "DDDDADD6-BCB5-4E5D-830E-242B8557E5CB";

        public const string LOOSE_IMAGES_BACK = "EEEEADD6-BCB5-4E5D-830E-242B8557E5CB";

        public const string LOOSE_IMAGES_ARTIST = "EEEFB4A9-49E3-4A1C-932F-E3A3B97BAD08";

        public const string LOOSE_IMAGES_FOLDER = "EEFFCABF-0B44-4F19-860F-A5B97050885A";

        public const string IMAGES_PREFERENCE_EMBEDDED = "AAAA9180-7C4E-4038-8BA8-60D3516E64E8";

        public const string IMAGES_PREFERENCE_LOOSE = "BBBB2170-3E86-4255-9BC6-EF31D870741E";

        public const string MAX_IMAGE_SIZE = "EEFFD218-2EF5-4256-A6FE-A9266BBEEC72";

        public const string COPY_IMAGES_ELEMENT = "FFFFA875-8195-49AF-A1A4-2EAB2294A6D8";

        public const string READ_EXTENDED_TAGS = "GGGG9A48-2D0A-4FFA-9749-4592BDFFF5DE";

        public const string READ_MUSICBRAINZ_TAGS = "HHHH0261-12E4-4550-B926-67104695F2F7";

        public const string READ_LYRICS_TAGS = "IIII124F-FA35-4D35-B1A9-A2EF8E189A4F";

        public const string READ_REPLAY_GAIN_TAGS = "IIJJF73C-4747-4961-9BFA-80639C31B9C7";

        public const string READ_POPULARIMETER_TAGS = "JJJJ6988-D3BF-434F-B326-3354D2922926";

        public const string READ_WINDOWS_MEDIA_TAGS = "JJKK0057-52AD-4914-BF4A-692EF76C7C83";

        public const string READ_DOCUMENTS = "JJJK51C0-D530-44B8-B9E9-7280041C0524";

        public const string READ_FILESYSTEM = "JJKK6ADD-C5BE-4A1C-B315-FFE903AC181B";

        public const string DETECT_COMPILATIONS = "JJKK150D-DB8D-4C64-99E4-10B453B26295";

        public const string THREADS_ELEMENT = "KKKK16BD-B4A7-4F9D-B4DA-F81E932F6DD9";

        public const string WRITE_ELEMENT = "LLLLEBD5-2675-42E4-8C9A-6E851DAA4D86";

        public const string WRITE_NONE_OPTION = "AAAA73B6-7263-42A3-8304-9EAD8FB82197";

        public const string WRITE_TAGS_OPTION = "BBBB6140-1201-4120-B2A9-E3BDBB582D47";

        public const string WRITE_TAGS_AND_STATISTICS_OPTION = "CCCC75D8-4BCA-41E4-99A1-540F78B170EE";

        public const string BACKGROUND_WRITE_ELEMENT = "MMMMF522-A8F2-42A9-8F48-6EE98775E34E";

        public static IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            yield return new ConfigurationSection(SECTION, Strings.MetaDataBehaviourConfiguration_Section)
                .WithElement(
                    new BooleanConfigurationElement(READ_EMBEDDED_IMAGES, Strings.MetaDataBehaviourConfiguration_EmbeddedImages).WithValue(true))
                .WithElement(
                    new BooleanConfigurationElement(READ_LOOSE_IMAGES, Strings.MetaDataBehaviourConfiguration_LooseImages).WithValue(true))
                .WithElement(
                    new TextConfigurationElement(LOOSE_IMAGES_FRONT, Strings.MetaDataBehaviourConfiguration_FrontCover, path: Strings.General_Advanced).WithValue("front, cover, folder").DependsOn(SECTION, READ_LOOSE_IMAGES))
                .WithElement(
                    new TextConfigurationElement(LOOSE_IMAGES_BACK, Strings.MetaDataBehaviourConfiguration_BackCover, path: Strings.General_Advanced).WithValue("back").DependsOn(SECTION, READ_LOOSE_IMAGES))
                .WithElement(
                    new TextConfigurationElement(LOOSE_IMAGES_ARTIST, Strings.MetaDataBehaviourConfiguration_Artist, path: Strings.General_Advanced).WithValue("artist").DependsOn(SECTION, READ_LOOSE_IMAGES))
                .WithElement(
                    new TextConfigurationElement(LOOSE_IMAGES_FOLDER, Strings.MetaDataBehaviourConfiguration_Folder, path: Strings.General_Advanced).WithValue("covers").DependsOn(SECTION, READ_LOOSE_IMAGES))
                .WithElement(
                    new SelectionConfigurationElement(IMAGES_PREFERENCE, Strings.MetaDataBehaviourConfiguration_Preference, path: Strings.General_Advanced).WithOptions(GetImagesPreferenceOptions()).DependsOn(SECTION, READ_EMBEDDED_IMAGES).DependsOn(SECTION, READ_LOOSE_IMAGES))
                .WithElement(
                    new IntegerConfigurationElement(MAX_IMAGE_SIZE, Strings.MetaDataBehaviourConfiguration_ImageSize, path: Strings.General_Advanced).WithValue(4).WithValidationRule(new IntegerValidationRule(1, 16)))
                .WithElement(
                    new BooleanConfigurationElement(COPY_IMAGES_ELEMENT, Strings.MetaDataBehaviourConfiguration_ImageCopy, path: Strings.General_Advanced).WithValue(true))
                .WithElement(
                    new BooleanConfigurationElement(READ_EXTENDED_TAGS, Strings.MetaDataBehaviourConfiguration_Extended, path: Strings.General_Advanced).WithValue(false))
                .WithElement(
                    new BooleanConfigurationElement(READ_MUSICBRAINZ_TAGS, Strings.MetaDataBehaviourConfiguration_MusicBrainz, path: Strings.General_Advanced).WithValue(false))
                .WithElement(
                    new BooleanConfigurationElement(READ_LYRICS_TAGS, Strings.MetaDataBehaviourConfiguration_Lyrics).WithValue(Publication.ReleaseType == ReleaseType.Default))
                .WithElement(
                    new BooleanConfigurationElement(READ_REPLAY_GAIN_TAGS, Strings.MetaDataBehaviourConfiguration_ReplayGain).WithValue(Publication.ReleaseType == ReleaseType.Default))
                .WithElement(
                    new BooleanConfigurationElement(READ_POPULARIMETER_TAGS, Strings.MetaDataBehaviourConfiguration_Popularimeter).WithValue(Publication.ReleaseType == ReleaseType.Default))
                .WithElement(
                    new BooleanConfigurationElement(READ_WINDOWS_MEDIA_TAGS, Strings.MetaDataBehaviourConfiguration_WindowsMedia).WithValue(false))
                .WithElement(
                    new BooleanConfigurationElement(READ_DOCUMENTS, Strings.MetaDataBehaviourConfiguration_Documents).WithValue(false))
                .WithElement(
                    new BooleanConfigurationElement(READ_FILESYSTEM, Strings.MetaDataBehaviourConfiguration_FileSystem).WithValue(true))
                .WithElement(
                    new BooleanConfigurationElement(DETECT_COMPILATIONS, Strings.MetaDataBehaviourConfiguration_DetectCompilations).WithValue(Publication.ReleaseType == ReleaseType.Default))
                .WithElement(
                    new IntegerConfigurationElement(THREADS_ELEMENT, Strings.MetaDataBehaviourConfiguration_Threads, path: Strings.General_Advanced).WithValue(Math.Max(Environment.ProcessorCount, 4)).WithValidationRule(new IntegerValidationRule(1, 32)))
                .WithElement(
                    new SelectionConfigurationElement(WRITE_ELEMENT, Strings.MetaDataBehaviourConfiguration_Write, path: Strings.General_Advanced).WithOptions(GetWriteBehaviourOptions()))
                .WithElement(
                    new BooleanConfigurationElement(BACKGROUND_WRITE_ELEMENT, Strings.MetaDataBehaviourConfiguration_WriteBackground, path: Strings.General_Advanced).WithValue(Publication.ReleaseType == ReleaseType.Default)
            );
        }

        private static IEnumerable<SelectionConfigurationOption> GetImagesPreferenceOptions()
        {
            {
                var option = new SelectionConfigurationOption(IMAGES_PREFERENCE_EMBEDDED, Strings.MetaDataBehaviourConfiguration_EmbeddedImages);
                if (Publication.ReleaseType == ReleaseType.Default)
                {
                    option.Default();
                }
                yield return option;
            }
            {
                var option = new SelectionConfigurationOption(IMAGES_PREFERENCE_LOOSE, Strings.MetaDataBehaviourConfiguration_LooseImages);
                if (Publication.ReleaseType == ReleaseType.Minimal)
                {
                    option.Default();
                }
                yield return option;
            }
        }

        public static ImagePreference GetImagesPreference(SelectionConfigurationOption value)
        {
            if (value != null)
            {
                switch (value.Id)
                {
                    default:
                    case IMAGES_PREFERENCE_EMBEDDED:
                        return ImagePreference.Embedded;
                    case IMAGES_PREFERENCE_LOOSE:
                        return ImagePreference.Loose;
                }
            }
            return ImagePreference.Embedded;
        }

        private static IEnumerable<SelectionConfigurationOption> GetWriteBehaviourOptions()
        {
            yield return new SelectionConfigurationOption(
                WRITE_NONE_OPTION,
                Strings.MetaDataBehaviourConfiguration_WriteNone
            );
            yield return new SelectionConfigurationOption(
                WRITE_TAGS_OPTION,
                Strings.MetaDataBehaviourConfiguration_WriteTags
            ).Default();
            yield return new SelectionConfigurationOption(
                WRITE_TAGS_AND_STATISTICS_OPTION,
                Strings.MetaDataBehaviourConfiguration_WriteTagsAndStatistics
            );
        }

        public static WriteBehaviour GetWriteBehaviour(SelectionConfigurationOption value)
        {
            if (value != null)
            {
                switch (value.Id)
                {
                    case WRITE_NONE_OPTION:
                        return WriteBehaviour.None;
                    default:
                    case WRITE_TAGS_OPTION:
                        return WriteBehaviour.Tags;
                    case WRITE_TAGS_AND_STATISTICS_OPTION:
                        return WriteBehaviour.Tags | WriteBehaviour.Statistics;
                }
            }
            return WriteBehaviour.Tags;
        }
    }

    public enum ImagePreference : byte
    {
        None,
        Embedded,
        Loose
    }

    public enum WriteBehaviour : byte
    {
        None,
        Tags,
        Statistics
    }
}
