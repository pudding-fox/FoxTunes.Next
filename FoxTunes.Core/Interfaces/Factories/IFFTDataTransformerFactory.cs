namespace FoxTunes.Interfaces
{
    public interface IFFTDataTransformerFactory : IStandardFactory
    {
        IFFTDataTransformer Create(int[] bands);
    }
}
