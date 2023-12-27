namespace FoxTunes.Interfaces
{
    public interface IScriptingRuntime : IStandardComponent
    {
        string Id { get; }

        string Name { get; }

        string Description { get; }

        ICoreScripts CoreScripts { get; }

        IScriptingContext CreateContext();
    }
}
