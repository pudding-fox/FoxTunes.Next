using FoxTunes.Interfaces;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace FoxTunes
{
    public abstract class BaseComponent : IBaseComponent, IInitializable, IObservable
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public bool IsInitialized { get; private set; }

        public virtual void InitializeComponent(ICore core)
        {
            this.IsInitialized = true;
        }

        protected virtual void Dispatch(Action action)
        {
#if NET40
            var task = TaskEx.Run(() =>
#else
            var task = Task.Run(() =>
#endif
            {
                try
                {
                    action();
                }
                catch
                {
                    //Nothing can be done, never throw on background thread.
                }
            });
        }

        protected virtual void Dispatch(Func<Task> function)
        {
#if NET40
            var task = TaskEx.Run(() =>
#else
            var task = Task.Run(() =>
#endif
            {
                try
                {
                    return function();
                }
                catch
                {
                    //Nothing can be done, never throw on background thread.
#if NET40
                    return TaskEx.FromResult(false);
#else
                    return Task.CompletedTask;
#endif
                }
            });
        }

        protected virtual void OnPropertyChanging(string propertyName)
        {
            if (this.PropertyChanging == null)
            {
                return;
            }
            this.PropertyChanging(this, new PropertyChangingEventArgs(propertyName));
        }

        public event PropertyChangingEventHandler PropertyChanging;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged == null)
            {
                return;
            }
            this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
