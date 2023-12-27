using ManagedBass;
using System;

namespace FoxTunes.Interfaces
{
    public interface IBassStreamComponent : IBaseComponent, IInitializable, IDisposable
    {
        string Name { get; }

        string Description { get; }

        int ChannelHandle { get; }

        BassFlags Flags { get; }

        int BufferLength { get; }

        bool IsActive { get; }

        bool IsStarting { get; set; }

        bool IsStopping { get; set; }

        bool GetFormat(out int rate, out int channels, out BassFlags flags);

        void Connect(IBassStreamComponent previous);

        void ClearBuffer();

        event EventHandler Invalidate;
    }
}
