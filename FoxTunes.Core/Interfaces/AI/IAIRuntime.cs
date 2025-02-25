namespace FoxTunes.Interfaces
{
    public interface IAIRuntime : IStandardComponent
    {
        string Id { get; }

        string Name { get; }

        string Description { get; }

        IAIContext CreateContext();
    }
}
