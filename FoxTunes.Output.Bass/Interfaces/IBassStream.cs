using ManagedBass;
using System;
using System.Collections.Generic;

namespace FoxTunes.Interfaces
{
    public interface IBassStream : IDisposable
    {
        IBassStreamProvider Provider { get; }

        int ChannelHandle { get; }

        int[] Syncs { get; }

        long Length { get; }

        long Position { get; set; }

        bool IsInteractive { get; }

        bool IsEmpty { get; }

        bool IsPending { get; }

        bool IsEnded { get; }

        IEnumerable<IBassStreamAdvice> Advice { get; }

        BassFlags Flags { get; }

        Errors Errors { get; }

        event EventHandler Ending;

        event EventHandler Ended;

        void AddSyncHandlers();

        void RemoveSyncHandlers();

        bool CanReset { get; }

        void Reset();
    }
}
