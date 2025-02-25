using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IOutputStream : IBaseComponent, IDisposable
    {
        int Id { get; }

        string FileName { get; }

        PlaylistItem PlaylistItem { get; }

        long Position { get; }

        long ActualPosition { get; }

        long Length { get; }

        int Rate { get; }

        int Channels { get; }

        bool IsReady { get; }

        bool IsPlaying { get; }

        bool IsPaused { get; }

        bool IsStopped { get; }

        bool IsEnded { get; }

        Task Play();

        Task Pause();

        Task Resume();

        Task Stop();

        Task Seek(long position);

        event EventHandler Ending;

        event EventHandler Ended;

        string Description { get; }

        Task BeginSeek();

        Task EndSeek();

        TimeSpan GetDuration(long position);

        OutputStreamFormat Format { get; }

        T[] GetBuffer<T>(TimeSpan duration) where T : struct;

        int GetData(short[] buffer);

        int GetData(float[] buffer);

        bool CanReset { get; }

        void Reset();

        bool IsDisposed { get; }
    }

    public enum OutputStreamFormat : byte
    {
        None,
        Short,
        Float,
        DSDRaw
    }
}
