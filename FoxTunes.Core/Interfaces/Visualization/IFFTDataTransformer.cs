namespace FoxTunes.Interfaces
{
    public interface IFFTDataTransformer : IBaseComponent
    {
        void Transform(FFTVisualizationData source, float[] values, float[] peakValues, float[] rmsValues);
    }
}
