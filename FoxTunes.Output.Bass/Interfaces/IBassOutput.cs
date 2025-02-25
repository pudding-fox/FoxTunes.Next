using System;

namespace FoxTunes.Interfaces
{
    public interface IBassOutput : IOutput, IConfigurableComponent, IDisposable
    {
        int Rate { get; }

        bool EnforceRate { get; }

        bool Float { get; }

        int UpdatePeriod { get; }

        int UpdateThreads { get; }

        int BufferLength { get; }

        int MixerBufferLength { get; }

        int ResamplingQuality { get; }

        event EventHandler Init;

        event EventHandler Free;
    }
}
