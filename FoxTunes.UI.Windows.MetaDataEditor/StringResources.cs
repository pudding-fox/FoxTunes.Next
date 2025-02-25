namespace FoxTunes
{
    /// <summary>
    /// This class just exposes the internal properties of <see cref="Strings"/> so we don't need to use PublicResXFileCodeGenerator.
    /// PublicResXFileCodeGenerator causes many type import conflict warnings.
    /// </summary>
    public static class StringResources
    {
        public static string MetaDataEntry_NoValue
        {
            get
            {
                return Strings.MetaDataEntry_NoValue;
            }
        }

        public static string MetaDataEntry_MultipleValues
        {
            get
            {
                return Strings.MetaDataEntry_MultipleValues;
            }
        }
    }
}
