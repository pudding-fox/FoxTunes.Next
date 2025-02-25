using FoxTunes.Interfaces;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class FFTDataTransformerFactory : StandardFactory, IFFTDataTransformerFactory
    {
        public IFFTDataTransformer Create(int[] bands)
        {
            return new FFTDataTransformer(bands);
        }
    }
}
