using System;

namespace FoxTunes.Interfaces
{
    public interface IVisualizationDataSource : IStandardComponent
    {
        bool Update(PCMVisualizationData data);

        bool Update(FFTVisualizationData data);
    }

    public abstract class VisualizationData : BaseComponent
    {
        public float[,] Data;

        public float[] Peak;

        public int Rate;

        public int Channels;

        public OutputStreamFormat Format;

        public bool Initialized;

        public VisualizationDataFlags Flags;

        public VisualizationDataHistory History;

        public virtual void OnAllocated()
        {
            //Nothing to do.
        }
    }

    [Flags]
    public enum VisualizationDataFlags : byte
    {
        None = 0,
        Individual = 1
    }

    public class PCMVisualizationData : VisualizationData
    {
        public TimeSpan Interval;

        public short[] Samples16;

        public float[] Samples32;

        public int SampleCount;
    }

    public class FFTVisualizationData : VisualizationData
    {
        public TimeSpan Interval;

        public int FFTSize;

        public float[] Samples;

        public int SampleCount;
    }

    public class VisualizationDataHistory
    {
        public float[,,] Values;

        public float[,] Avg;

        public float[,] Peak;

        public float[,] Rms;

        public int Position;

        public int Count;

        public int Capacity;

        public VisualizationDataHistoryFlags Flags;

        public virtual void OnAllocated()
        {
            //Nothing to do.
        }
    }

    [Flags]
    public enum VisualizationDataHistoryFlags : byte
    {
        None = 0,
        Average = 1,
        Peak = 2,
        Rms = 4
    }
}
