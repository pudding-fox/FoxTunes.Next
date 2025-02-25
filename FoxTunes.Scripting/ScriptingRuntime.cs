using FoxTunes.Interfaces;

namespace FoxTunes
{
    public abstract class ScriptingRuntime : StandardComponent, IScriptingRuntime
    {
        protected ScriptingRuntime(string id, string name = null, string description = null)
        {
            this.Id = id;
            this.Name = name;
            this.Description = description;
        }

        public string Id { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public abstract ICoreScripts CoreScripts { get; }

        public abstract IScriptingContext CreateContext();
    }
}
