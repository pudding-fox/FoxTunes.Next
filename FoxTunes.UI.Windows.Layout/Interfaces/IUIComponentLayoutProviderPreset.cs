namespace FoxTunes.Interfaces
{
    public interface IUIComponentLayoutProviderPreset : IStandardComponent
    {
        string Id { get; }

        string Category { get; }

        string Name { get; }

        string Layout { get; }
    }
}
