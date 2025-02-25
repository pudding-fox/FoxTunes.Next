using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;

namespace FoxTunes.ViewModel
{
    public class AsyncCommand : CommandBase
    {
        public AsyncCommand(Func<Task> func)
        {
            this.Func = func;
        }

        public AsyncCommand(Func<Task> func, Func<bool> predicate)
            : this(func)
        {
            this.Predicate = predicate;
        }

        public Func<Task> Func { get; private set; }

        public Func<bool> Predicate { get; private set; }

        public override bool CanExecute(object parameter)
        {
            if (this.Predicate != null)
            {
                return this.Predicate();
            }
            return true;
        }

        public override void Execute(object parameter)
        {
            if (this.Func == null)
            {
                return;
            }
#if NET40
            var task = TaskEx.Run(async () =>
#else
            var task = Task.Run(async () =>
#endif
            {
                try
                {
                    await this.Func().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(AsyncCommand), LogLevel.Warn, "Failed to execute command: {0}", e.Message);
                    await ErrorEmitter.Send(this, string.Format("Failed to execute command: {0}", e.Message), e).ConfigureAwait(false);
                }
                return Windows.Invoke(() => this.OnCanExecuteChanged());
            });
        }
    }

    public class AsyncCommand<T> : CommandBase
    {
        public AsyncCommand(Func<T, Task> func)
        {
            this.Func = func;
        }

        public AsyncCommand(Func<T, Task> func, Func<T, bool> predicate)
            : this(func)
        {
            this.Predicate = predicate;
        }

        public Func<T, Task> Func { get; private set; }

        public Func<T, bool> Predicate { get; private set; }

        public override bool CanExecute(object parameter)
        {
            if (this.Predicate != null)
            {
                if (parameter is T)
                {
                    return this.Predicate((T)parameter);
                }
                else
                {
                    return this.Predicate(default(T));
                }
            }
            return true;
        }

        public override void Execute(object parameter)
        {
            if (this.Func == null)
            {
                return;
            }
#if NET40
            var task = TaskEx.Run(async () =>
#else
            var task = Task.Run(async () =>
#endif
            {
                try
                {
                    if (parameter is T)
                    {
                        await this.Func((T)parameter).ConfigureAwait(false);
                    }
                    else
                    {
                        await this.Func(default(T)).ConfigureAwait(false);
                    }
                }
                catch (Exception e)
                {
                    Logger.Write(typeof(AsyncCommand), LogLevel.Warn, "Failed to execute command: {0}", e.Message);
                    await ErrorEmitter.Send(this, string.Format("Failed to execute command: {0}", e.Message), e).ConfigureAwait(false);
                }
                return Windows.Invoke(() => this.OnCanExecuteChanged());
            });
        }
    }
}
