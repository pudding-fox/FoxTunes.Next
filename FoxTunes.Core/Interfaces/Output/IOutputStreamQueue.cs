using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IOutputStreamQueue : IStandardComponent, IDisposable
    {
        bool IsQueued(PlaylistItem playlistItem);

        bool Requeue(PlaylistItem playlistItem);

        IOutputStream Peek(PlaylistItem playlistItem);

        Task Enqueue(IOutputStream outputStream, bool dequeue);

        Task Dequeue(PlaylistItem playlistItem);

        event OutputStreamQueueEventHandler Dequeued;
    }

    public delegate void OutputStreamQueueEventHandler(object sender, OutputStreamQueueEventArgs e);

    public class OutputStreamQueueEventArgs : AsyncEventArgs
    {
        public OutputStreamQueueEventArgs(IOutputStream outputStream)
        {
            this.OutputStream = outputStream;
        }

        public IOutputStream OutputStream { get; private set; }
    }
}
