using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IAIRuntime : IStandardComponent
    {
        string Id { get; }

        string Name { get; }

        string Description { get; }

        ICorePrompts CorePrompts { get; }

        IAIContext CreateContext(IEnumerable<IAIPrompt> prompts);
    }
}
