namespace FoxTunes
{
    public class BooleanConfigurationElement : ConfigurationElement<bool>
    {
        public BooleanConfigurationElement(string id, string name = null, string description = null, string path = null)
            : base(id, name, description, path)
        {
        }

        public void Toggle()
        {
            this.Value = !this.Value;
        }
    }
}