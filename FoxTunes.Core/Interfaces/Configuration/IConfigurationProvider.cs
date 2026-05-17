namespace FoxTunes.Interfaces
{
    public interface IConfigurationBaseProvider
    {
        IConfigurationBase GetConfiguration(IConfigurableComponent component);
    }
}
