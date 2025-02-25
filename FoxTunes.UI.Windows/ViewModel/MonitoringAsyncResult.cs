using FoxTunes.Interfaces;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class MonitoringAsyncResult<T> : Wrapper<T>, IWeakEventListener where T : class
    {
        private MonitoringAsyncResult()
        {

        }

        public MonitoringAsyncResult(IObservable source, Func<Task<T>> factory) : this()
        {
            this.Source = source;
            PropertyChangedEventManager.AddListener(source, this, string.Empty);
            this.Factory = factory;
            this.Dispatch(this.Run);
        }

        public MonitoringAsyncResult(IObservable source, T value, Func<Task<T>> factory) : this()
        {
            this.Source = source;
            PropertyChangedEventManager.AddListener(source, this, string.Empty);
            this.Value = value;
            this.Factory = factory;
            this.Dispatch(this.Run);
        }

        public IObservable Source { get; private set; }

        public Func<Task<T>> Factory { get; private set; }

        public async Task Run()
        {
            var task = this.Factory();
            var value = await task.ConfigureAwait(false);
            await Windows.Invoke(() => this.Value = value).ConfigureAwait(false);
        }

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (e is PropertyChangedEventArgs propertyChangedEventArgs && string.IsNullOrEmpty(propertyChangedEventArgs.PropertyName))
            {
                this.Dispatch(this.Run);
            }
            return true;
        }

        protected override void OnDisposing()
        {
            PropertyChangedEventManager.RemoveListener(this.Source, this, string.Empty);
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new MonitoringAsyncResult<T>();
        }
    }
}
