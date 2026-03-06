using FoxTunes.Interfaces;

namespace FoxTunes
{
    public interface IRendererTargetBehaviour : IStandardComponent, IConfigurableComponent
    {
        string Id { get; }

        RendererTarget Create(int width, int height);
    }
}
