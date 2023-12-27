using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class OutputStreamQueue : StandardComponent, IOutputStreamQueue
    {
        public static readonly object SyncRoot = new object();

        public OutputStreamQueue()
        {
            this.Queue = new ConcurrentDictionary<PlaylistItem, OutputStreamQueueValue>();
        }

        private ConcurrentDictionary<PlaylistItem, OutputStreamQueueValue> Queue { get; set; }

        public IConfiguration Configuration { get; private set; }

        public IntegerConfigurationElement Count { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            if (core.Components.Output != null)
            {
                core.Components.Output.IsStartedChanged += (sender, e) =>
                {
                    if (this.Queue.Count > 0)
                    {
                        Logger.Write(this, LogLevel.Warn, "Output state changed, disposing queued output streams.");
                        this.Clear();
                    }
                };
            }
            this.Configuration = core.Components.Configuration;
            this.Count = this.Configuration.GetElement<IntegerConfigurationElement>(
                PlaybackBehaviourConfiguration.SECTION,
                EnqueueNextItemBehaviourConfiguration.COUNT
            );
            base.InitializeComponent(core);
        }

        public bool IsQueued(PlaylistItem playlistItem)
        {
            lock (SyncRoot)
            {
                return this.Queue.ContainsKey(playlistItem);
            }
        }

        public bool Requeue(PlaylistItem playlistItem)
        {
            lock (SyncRoot)
            {
                var value = default(OutputStreamQueueValue);
                if (!this.Queue.TryGetValue(playlistItem, out value))
                {
                    return false;
                }
                value.CreatedAt = DateTime.UtcNow;
                return true;
            }
        }

        public IOutputStream Peek(PlaylistItem playlistItem)
        {
            lock (SyncRoot)
            {
                var value = default(OutputStreamQueueValue);
                if (!this.Queue.TryGetValue(playlistItem, out value))
                {
                    return default(IOutputStream);
                }
                return value.OutputStream;
            }
        }

        public Task Enqueue(IOutputStream outputStream, bool dequeue)
        {
            lock (SyncRoot)
            {
                if (!this.Queue.TryAdd(outputStream.PlaylistItem, new OutputStreamQueueValue(outputStream)))
                {
                    throw new InvalidOperationException("Failed to add the specified output stream to the queue.");
                }
                this.EnsureCapacity(outputStream.PlaylistItem);
                if (!dequeue)
                {
#if NET40
                    return TaskEx.FromResult(false);
#else
                    return Task.CompletedTask;
#endif
                }
                return this.Dequeue(outputStream.PlaylistItem);
            }
        }

        private void EnsureCapacity(PlaylistItem playlistItem)
        {
            lock (SyncRoot)
            {
                //We remove the oldest items in the queue to enforce the capacity.
                //Hopefully they are not needed.
                var query =
                    from pair in this.Queue
                    where pair.Key != playlistItem
                    orderby pair.Value.CreatedAt descending
                    select pair.Key;
                while (this.Queue.Count > this.Count.Value)
                {
                    var key = query.FirstOrDefault();
                    if (key == null)
                    {
                        return;
                    }
                    Logger.Write(this, LogLevel.Debug, "Evicting output stream from the queue due to exceeded capacity: {0} => {1}", key.Id, key.FileName);
                    var value = default(OutputStreamQueueValue);
                    if (!this.Queue.TryRemove(key, out value))
                    {
                        continue;
                    }
                    value.OutputStream.Dispose();
                }
            }
        }

        public Task Dequeue(PlaylistItem playlistItem)
        {
            var value = default(OutputStreamQueueValue);
            lock (SyncRoot)
            {
                if (!this.Queue.TryRemove(playlistItem, out value))
                {
                    throw new InvalidOperationException("Failed to locate the specified playlist item in the queue.");
                }
            }
            return this.OnDequeued(value.OutputStream);
        }

        protected virtual Task OnDequeued(IOutputStream outputStream)
        {
            if (this.Dequeued == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            var e = new OutputStreamQueueEventArgs(outputStream);
            this.Dequeued(this, e);
            return e.Complete();
        }

        public event OutputStreamQueueEventHandler Dequeued;

        public void Clear()
        {
            lock (SyncRoot)
            {
                foreach (var pair in this.Queue)
                {
                    var outputStream = pair.Value.OutputStream;
                    Logger.Write(this, LogLevel.Debug, "Disposing queued output stream: {0} => {1}", outputStream.Id, outputStream.FileName);
                    outputStream.Dispose();
                }
                Logger.Write(this, LogLevel.Debug, "Clearing output stream queue.");
                this.Queue.Clear();
            }
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            this.Clear();
        }

        ~OutputStreamQueue()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        private class OutputStreamQueueValue
        {
            private OutputStreamQueueValue()
            {
                this.CreatedAt = DateTime.UtcNow;
            }

            public OutputStreamQueueValue(IOutputStream outputStream)
                : this()
            {
                this.OutputStream = outputStream;
            }

            public IOutputStream OutputStream { get; private set; }

            public DateTime CreatedAt { get; set; }
        }
    }
}
