using System.Threading.Tasks;

namespace FoxTunes.ViewModel
{
    public class AsyncResult<T> : Wrapper<T> where T : class
    {
        private AsyncResult()
        {

        }

        private AsyncResult(T value)
        {
            this.Value = value;
        }

        public AsyncResult(Task<T> task) : this()
        {
            this.Task = task;
            this.Dispatch(this.Run);
        }

        public AsyncResult(T value, Task<T> task) : this()
        {
            this.Value = value;
            this.Task = task;
            this.Dispatch(this.Run);
        }

        public Task<T> Task { get; private set; }

        public async Task Run()
        {
            var value = await this.Task.ConfigureAwait(false);
            await Windows.Invoke(() => this.Value = value).ConfigureAwait(false);
        }

        public static AsyncResult<T> FromValue(T value)
        {
            return new AsyncResult<T>(value);
        }
    }
}
