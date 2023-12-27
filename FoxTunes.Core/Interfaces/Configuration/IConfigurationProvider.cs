namespace FoxTunes.Interfaces
{
    public interface IConfigurationProvider
    {
        IConfiguration GetConfiguration(IConfigurableComponent component);
    }
}
