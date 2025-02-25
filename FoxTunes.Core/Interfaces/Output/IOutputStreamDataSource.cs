using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IOutputStreamDataSource
    {
        IOutputStream Stream { get; }

        bool GetFormat(out int rate, out int channels, out OutputStreamFormat format);

        bool GetChannelMap(out IDictionary<int, OutputChannel> channels);

        T[] GetBuffer<T>(TimeSpan duration) where T : struct;

        int GetData(short[] buffer);

        int GetData(float[] buffer);

        float[] GetBuffer(int fftSize, bool individual = false);

        int GetData(float[] buffer, int fftSize, bool individual = false);

        int GetData(float[] buffer, int fftSize, out TimeSpan duration, bool individual = false);
    }
}
