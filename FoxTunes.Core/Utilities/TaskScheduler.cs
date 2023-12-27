using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class TaskScheduler : global::System.Threading.Tasks.TaskScheduler
    {
        [ThreadStatic]
        public static volatile bool CurrentThreadIsProcessingItems;

        private TaskScheduler()
        {
            this.Tasks = new LinkedList<Task>();
        }

        public TaskScheduler(ParallelOptions options) : this()
        {
            this.Options = options;
        }

        public LinkedList<Task> Tasks { get; private set; }

        public ParallelOptions Options { get; private set; }

        public int Count { get; private set; }

        public sealed override int MaximumConcurrencyLevel
        {
            get
            {
                return this.Options.MaxDegreeOfParallelism;
            }
        }

        protected override void QueueTask(Task task)
        {
            lock (this.Tasks)
            {
                this.Tasks.AddLast(task);
                if (this.Count < this.MaximumConcurrencyLevel)
                {
                    this.Count++;
                    this.NotifyThreadPoolOfPendingWork();
                }
            }
        }

        private void NotifyThreadPoolOfPendingWork()
        {
            ThreadPool.UnsafeQueueUserWorkItem(_ =>
            {
                CurrentThreadIsProcessingItems = true;
                try
                {
                    while (true)
                    {
                        var item = default(Task);
                        lock (this.Tasks)
                        {
                            if (this.Tasks.Count == 0)
                            {
                                this.Count--;
                                break;
                            }

                            item = this.Tasks.First.Value;
                            this.Tasks.RemoveFirst();
                        }
                        base.TryExecuteTask(item);
                    }
                }
                finally
                {
                    CurrentThreadIsProcessingItems = false;
                }
            }, null);
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            if (!CurrentThreadIsProcessingItems)
            {
                return false;
            }

            if (taskWasPreviouslyQueued)
            {
                this.TryDequeue(task);
            }

            return base.TryExecuteTask(task);
        }

        protected override bool TryDequeue(Task task)
        {
            lock (this.Tasks)
            {
                return this.Tasks.Remove(task);
            }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            lock (this.Tasks)
            {
                return this.Tasks.ToArray();
            }
        }
    }
}
