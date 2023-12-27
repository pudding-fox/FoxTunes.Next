using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IBackgroundTask : IBaseComponent, IReportsProgress, ICancellable, IDisposable
    {
        string Id { get; }

        bool Visible { get; }

        event EventHandler Started;

        bool IsStarted { get; }

        event EventHandler Completed;

        bool IsCompleted { get; }

        Exception Exception { get; }

        event EventHandler ExceptionChanged;

        event EventHandler Faulted;

        bool IsFaulted { get; }

        Task Run();

        bool Cancellable { get; }

        void Cancel();
    }
}
