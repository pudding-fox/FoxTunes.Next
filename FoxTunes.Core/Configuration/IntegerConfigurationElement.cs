namespace FoxTunes
{
    public class IntegerConfigurationElement : ConfigurationElement<int>
    {
        public IntegerConfigurationElement(string id, string name = null, string description = null, string path = null) : base(id, name, description, path)
        {
        }
    }
}