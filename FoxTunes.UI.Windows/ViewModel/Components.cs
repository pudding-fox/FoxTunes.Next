using FoxTunes.Interfaces;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class Components : ViewModelBase
    {
        public Components()
        {
            this.Errors = new ObservableCollection<ComponentError>();
        }

        public ObservableCollection<ComponentError> Errors { get; set; }

        public bool _Enabled { get; private set; }

        public bool Enabled
        {
            get
            {
                return this._Enabled;
            }
            set
            {
                this._Enabled = value;
                this.OnEnabledChanged();
            }
        }

        protected virtual void OnEnabledChanged()
        {
            if (this.EnabledChanged != null)
            {
                this.EnabledChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Enabled");
        }

        public event EventHandler EnabledChanged;

        public IBackgroundTaskEmitter BackgroundTaskEmitter { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        protected override void InitializeComponent(ICore core)
        {
            this.BackgroundTaskEmitter = core.Components.BackgroundTaskEmitter;
            this.BackgroundTaskEmitter.BackgroundTask += this.OnBackgroundTask;
            this.ErrorEmitter = core.Components.ErrorEmitter;
            this.ErrorEmitter.Error += this.OnError;
            base.InitializeComponent(core);
        }

        protected virtual async Task OnError(object sender, ComponentErrorEventArgs e)
        {
            if (e.Exception is AggregateException aggregateException)
            {
                if (aggregateException.InnerExceptions.Count == 1)
                {
                    var innerException = aggregateException.InnerExceptions[0];
                    await this.Add(new ComponentError(e.Source, innerException.Message, innerException)).ConfigureAwait(false);
                }
                else
                {
                    foreach (var innerException in aggregateException.InnerExceptions)
                    {
                        await this.Add(new ComponentError(e.Source, innerException.Message, innerException)).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await this.Add(new ComponentError(e.Source, e.Message, e.Exception)).ConfigureAwait(false);
            }
        }

        protected virtual Task OnBackgroundTask(object sender, IBackgroundTask backgroundTask)
        {
            backgroundTask.Faulted += this.OnFaulted;
            backgroundTask.Completed += this.OnCompleted;
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual async void OnFaulted(object sender, EventArgs e)
        {
            var backgroundTask = sender as IBackgroundTask;
            if (backgroundTask.Exception is AggregateException aggregateException)
            {
                if (aggregateException.InnerExceptions.Count == 1)
                {
                    await this.Add(new ComponentError(backgroundTask, aggregateException.InnerExceptions[0].Message, aggregateException.InnerExceptions[0])).ConfigureAwait(false);
                }
                else
                {
                    foreach (var innerException in aggregateException.InnerExceptions)
                    {
                        await this.Add(new ComponentError(backgroundTask, innerException.Message, innerException)).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                await this.Add(new ComponentError(backgroundTask, backgroundTask.Exception.Message, backgroundTask.Exception)).ConfigureAwait(false);
            }
            backgroundTask.Faulted -= this.OnFaulted;
            backgroundTask.Completed -= this.OnCompleted;
        }

        protected virtual void OnCompleted(object sender, EventArgs e)
        {
            var backgroundTask = sender as IBackgroundTask;
            backgroundTask.Faulted -= this.OnFaulted;
            backgroundTask.Completed -= this.OnCompleted;
        }


        public Task Add(ComponentError error)
        {
            if (!this.Enabled)
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            return Windows.Invoke(() =>
            {
                if (this.Errors.Contains(error))
                {
                    return;
                }
                this.Errors.Add(error);
                //WPF glitch; ItemsControl in a Popup displays duplicate items.
                CollectionViewSource.GetDefaultView(this.Errors).Refresh();
            });
        }

        public ICommand ClearErrorsCommand
        {
            get
            {
                return new Command(() => this.Errors.Clear());
            }
        }

        protected override void OnDisposing()
        {
            if (this.BackgroundTaskEmitter != null)
            {
                this.BackgroundTaskEmitter.BackgroundTask -= this.OnBackgroundTask;
            }
            if (this.ErrorEmitter != null)
            {
                this.ErrorEmitter.Error -= this.OnError;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Components();
        }
    }

    public class ComponentError : IEquatable<ComponentError>
    {
        public ComponentError(IBaseComponent component, string message, Exception exception)
        {
            this.Component = component;
            this.Message = message;
            this.Exception = exception;
        }

        public IBaseComponent Component { get; private set; }

        public string Source
        {
            get
            {
                return this.Component.GetType().Name;
            }
        }

        public string Message { get; private set; }

        public Exception Exception { get; private set; }

        public override int GetHashCode()
        {
            var hashCode = 0;
            unchecked
            {
                if (!string.IsNullOrEmpty(this.Message))
                {
                    hashCode += this.Message.GetHashCode();
                }
                if (this.Exception != null && !string.IsNullOrEmpty(this.Exception.Message))
                {
                    hashCode += this.Exception.Message.GetHashCode();
                }
            }
            return hashCode;
        }

        public override bool Equals(object other)
        {
            if (other is ComponentError)
            {
                return this.Equals(other as ComponentError);
            }
            return base.Equals(other);
        }

        public bool Equals(ComponentError other)
        {
            if (other == null)
            {
                return false;
            }
            if (!string.Equals(this.Message, other.Message, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (this.Exception != null && other.Exception == null)
            {
                return false;
            }
            if (this.Exception == null && other.Exception != null)
            {
                return false;
            }
            if (!string.Equals(this.Exception.Message, other.Exception.Message, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }
    }
}
