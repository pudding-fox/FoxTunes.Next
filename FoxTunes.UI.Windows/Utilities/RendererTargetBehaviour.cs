using System.Collections.Generic;

namespace FoxTunes
{
    public abstract class RendererTargetBehaviour : StandardBehaviour, IRendererTargetBehaviour
    {
        public abstract string Id { get; }

        public abstract RendererTarget Create(int width, int height);

        public abstract IEnumerable<ConfigurationSection> GetConfigurationSections();
    }
}
