using FoxTunes.Interfaces;

namespace FoxTunes
{
    public class FFTDataTransformerFactory : StandardFactory, IFFTDataTransformerFactory
    {
        public IFFTDataTransformer Create(int[] bands)
        {
            return new FFTDataTransformer(bands);
        }
    }
}
