using ManagedBass;
using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IBassStreamOutput : IBassStreamControllable, IBassStreamComponent
    {
        bool IsPlaying { get; }

        bool IsPaused { get; }

        bool IsStopped { get; }

        bool CheckFormat(int rate, int channels);

        bool CanGetData { get; }

        bool GetDataFormat(out int rate, out int channels, out BassFlags flags);

        int GetData(short[] buffer);

        int GetData(float[] buffer);

        int GetData(float[] buffer, int fftSize, bool individual = false);

        int GetData(float[] buffer, int fftSize, out TimeSpan duration, bool individual = false);
    }
}
