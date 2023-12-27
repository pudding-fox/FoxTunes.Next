using FoxTunes.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.Output)]
    public class BassStreamPipelineManager : StandardComponent, IBassStreamPipelineManager
    {
        public const int SYNCHRONIZE_PIPELINE_TIMEOUT = 10000;

        public BassStreamPipelineManager()
        {
            this.Semaphore = new SemaphoreSlim(1, 1);
        }

        public SemaphoreSlim Semaphore { get; private set; }

        public IBassStreamPipelineFactory PipelineFactory { get; private set; }

        public IBassStreamPipeline Pipeline { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PipelineFactory = ComponentRegistry.Instance.GetComponent<IBassStreamPipelineFactory>();
            base.InitializeComponent(core);
        }

        public void WithPipeline(Action<IBassStreamPipeline> action)
        {
            action(this.Pipeline);
        }

        public T WithPipeline<T>(Func<IBassStreamPipeline, T> func)
        {
            return func(this.Pipeline);
        }

#if NET40

        public Task<bool> WithPipelineExclusive(Action<IBassStreamPipeline> action, int timeout = SYNCHRONIZE_PIPELINE_TIMEOUT)
        {
            if (!this.Semaphore.Wait(timeout))
            {
                return TaskEx.FromResult(false);
            }
            try
            {
                this.WithPipeline(action);
            }
            finally
            {
                this.Semaphore.Release();
            }
            return TaskEx.FromResult(true);
        }

#else

        public async Task<bool> WithPipelineExclusive(Action<IBassStreamPipeline> action, int timeout = SYNCHRONIZE_PIPELINE_TIMEOUT)
        {
            if (!await this.Semaphore.WaitAsync(timeout).ConfigureAwait(false))
            {
                return false;
            }
            try
            {
                this.WithPipeline(action);
            }
            finally
            {
                this.Semaphore.Release();
            }
            return true;
        }

#endif

#if NET40

        public Task<T> WithPipelineExclusive<T>(Func<IBassStreamPipeline, T> func, int timeout = SYNCHRONIZE_PIPELINE_TIMEOUT)
        {
            if (!this.Semaphore.Wait(timeout))
            {
                return TaskEx.FromResult(default(T));
            }
            try
            {
                return TaskEx.FromResult(this.WithPipeline(func));
            }
            finally
            {
                this.Semaphore.Release();
            }
        }

#else

        public async Task<T> WithPipelineExclusive<T>(Func<IBassStreamPipeline, T> func, int timeout = SYNCHRONIZE_PIPELINE_TIMEOUT)
        {
            if (!await this.Semaphore.WaitAsync(timeout).ConfigureAwait(false))
            {
                return default(T);
            }
            try
            {
                return this.WithPipeline(func);
            }
            finally
            {
                this.Semaphore.Release();
            }
        }

#endif

#if NET40

        public async Task<bool> WithPipelineExclusive(BassOutputStream stream, Action<IBassStreamPipeline> action, int timeout = SYNCHRONIZE_PIPELINE_TIMEOUT)
        {
            if (!this.Semaphore.Wait(timeout))
            {
                return false;
            }
            try
            {
                await this.WithPipeline(stream, action).ConfigureAwait(false);
            }
            finally
            {
                this.Semaphore.Release();
            }
            return true;
        }

#else

        public async Task<bool> WithPipelineExclusive(BassOutputStream stream, Action<IBassStreamPipeline> action, int timeout = SYNCHRONIZE_PIPELINE_TIMEOUT)
        {
            if (!await this.Semaphore.WaitAsync(timeout).ConfigureAwait(false))
            {
                return false;
            }
            try
            {
                await this.WithPipeline(stream, action).ConfigureAwait(false);
            }
            finally
            {
                this.Semaphore.Release();
            }
            return true;
        }

#endif

        protected virtual async Task WithPipeline(BassOutputStream stream, Action<IBassStreamPipeline> action)
        {
            if (this.Pipeline == null)
            {
                this.CreatePipelineCore(stream);
            }
            else if (!this.Pipeline.Input.CheckFormat(stream))
            {
                Logger.Write(this, LogLevel.Debug, "Current pipeline cannot accept stream, shutting it down: {0}", stream.ChannelHandle);
                this.FreePipelineCore();
                await this.WithPipeline(stream, action).ConfigureAwait(false);
                return;
            }
            action(this.Pipeline);
        }

#if NET40

        protected virtual Task<bool> CreatePipeline(BassOutputStream stream)
        {
            if (!this.Semaphore.Wait(SYNCHRONIZE_PIPELINE_TIMEOUT))
            {
                return TaskEx.FromResult(false);
            }
            try
            {
                this.CreatePipelineCore(stream);
            }
            finally
            {
                this.Semaphore.Release();
            }
            return TaskEx.FromResult(true);
        }

#else

        protected virtual async Task<bool> CreatePipeline(BassOutputStream stream)
        {
            if (!await this.Semaphore.WaitAsync(SYNCHRONIZE_PIPELINE_TIMEOUT).ConfigureAwait(false))
            {
                return false;
            }
            try
            {
                this.CreatePipelineCore(stream);
            }
            finally
            {
                this.Semaphore.Release();
            }
            return true;
        }

#endif

        protected virtual void CreatePipelineCore(BassOutputStream stream)
        {
            this.Pipeline = this.PipelineFactory.CreatePipeline(stream);
            this.Pipeline.IsStarting = true;
            this.Pipeline.Input.Add(stream, this.OnStreamAdded);
            this.Pipeline.IsStarting = false;
            this.OnCreated();
        }

        protected virtual void OnStreamAdded(BassOutputStream stream)
        {
            //Nothing to do.
        }

        protected virtual void OnCreated()
        {
            if (this.Created == null)
            {
                return;
            }
            this.Created(this, EventArgs.Empty);
        }

        public event EventHandler Created;

        protected virtual void OnDestroyed()
        {
            if (this.Destroyed == null)
            {
                return;
            }
            this.Destroyed(this, EventArgs.Empty);
        }

        public event EventHandler Destroyed;

#if NET40
        public Task FreePipeline()
#else
        public async Task FreePipeline()
#endif
        {
            if (this.Semaphore == null)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return;
#endif
            }
#if NET40
            if (!this.Semaphore.Wait(SYNCHRONIZE_PIPELINE_TIMEOUT))
#else
            if (!await this.Semaphore.WaitAsync(SYNCHRONIZE_PIPELINE_TIMEOUT).ConfigureAwait(false))
#endif
            {
                throw new InvalidOperationException(string.Format("{0} is already locked.", this.GetType().Name));
            }
            try
            {
                this.FreePipelineCore();
            }
            finally
            {
                this.Semaphore.Release();
            }
#if NET40
            return TaskEx.FromResult(false);
#endif
        }

        protected virtual void FreePipelineCore()
        {
            if (this.Pipeline != null)
            {
                Logger.Write(this, LogLevel.Debug, "Shutting down the pipeline.");
                this.Pipeline.IsStopping = true;
                this.Pipeline.Dispose();
                this.Pipeline.IsStopping = false;
                this.Pipeline = null;
                this.OnDestroyed();
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
            this.FreePipelineCore();
            if (this.Semaphore != null)
            {
                this.Semaphore.Dispose();
                this.Semaphore = null;
            }
        }

        ~BassStreamPipelineManager()
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
    }
}
