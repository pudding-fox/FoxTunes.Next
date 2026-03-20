using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    public abstract class AIRuntime : StandardComponent, IAIRuntime
    {
        protected AIRuntime(string id, string name = null, string description = null)
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public abstract ICorePrompts CorePrompts { get; }

        public abstract IAIContext CreateContext(IEnumerable<IAIPrompt> prompts);
    }
}
