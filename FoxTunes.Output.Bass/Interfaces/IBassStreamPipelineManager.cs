using System;
using System.Threading.Tasks;

namespace FoxTunes.Interfaces
{
    public interface IBassStreamPipelineManager : IBaseComponent, IDisposable
    {
        void WithPipeline(Action<IBassStreamPipeline> action);

        T WithPipeline<T>(Func<IBassStreamPipeline, T> func);

        Task<bool> WithPipelineExclusive(Action<IBassStreamPipeline> action, int timeout = BassStreamPipelineManager.SYNCHRONIZE_PIPELINE_TIMEOUT);

        Task<T> WithPipelineExclusive<T>(Func<IBassStreamPipeline, T> func, int timeout = BassStreamPipelineManager.SYNCHRONIZE_PIPELINE_TIMEOUT);

        Task<bool> WithPipelineExclusive(BassOutputStream stream, Action<IBassStreamPipeline> action, int timeout = BassStreamPipelineManager.SYNCHRONIZE_PIPELINE_TIMEOUT);

        Task FreePipeline();

        event EventHandler Created;

        event EventHandler Destroyed;
    }
}
