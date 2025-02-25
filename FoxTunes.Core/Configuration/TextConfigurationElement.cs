namespace FoxTunes
{
    public class TextConfigurationElement : ConfigurationElement<string>
    {
        public TextConfigurationElement(string id, string name = null, string description = null, string path = null)
            : base(id, name, description, path)
        {
        }
    }
}