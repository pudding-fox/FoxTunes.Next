using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IOutputDataSource : IStandardComponent
    {
        bool GetOutputFormat(out int rate, out int channels, out OutputStreamFormat format);

        bool GetOutputChannelMap(out IDictionary<int, OutputChannel> channels);

        bool CanGetData { get; }

        event EventHandler CanGetDataChanged;

        bool GetDataFormat(out int rate, out int channels, out OutputStreamFormat format);

        bool GetDataChannelMap(out IDictionary<int, OutputChannel> channels);

        T[] GetBuffer<T>(TimeSpan duration) where T : struct;

        int GetData(short[] buffer);

        int GetData(float[] buffer);

        float[] GetBuffer(int fftSize, bool individual = false);

        int GetData(float[] buffer, int fftSize, bool individual = false);

        int GetData(float[] buffer, int fftSize, out TimeSpan duration, bool individual = false);
    }
}
