namespace FoxTunes.ViewModel
{
    /// <summary>
    /// This class just exposes the internal properties of <see cref="Strings"/> so we don't need to use PublicResXFileCodeGenerator.
    /// PublicResXFileCodeGenerator causes many type import conflict warnings.
    /// </summary>
    public static class StringResources
    {
        public static string SelectionProperties_Tags
        {
            get
            {
                return Strings.SelectionProperties_Tags;
            }
        }

        public static string SelectionProperties_Properties
        {
            get
            {
                return Strings.SelectionProperties_Properties;
            }
        }

        public static string SelectionProperties_Location
        {
            get
            {
                return Strings.SelectionProperties_Location;
            }
        }

        public static string SelectionProperties_Images
        {
            get
            {
                return Strings.SelectionProperties_Images;
            }
        }

        public static string SelectionProperties_ReplayGain
        {
            get
            {
                return Strings.SelectionProperties_ReplayGain;
            }
        }
    }
}
