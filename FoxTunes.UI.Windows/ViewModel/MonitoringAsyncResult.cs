using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class MonitoringAsyncResult<T> : Wrapper<T>, IWeakEventListener where T : class
    {
        private MonitoringAsyncResult()
        {
            this.Tasks = new List<Task>();
        }

        public MonitoringAsyncResult(IObservable source, params Func<Task<T>>[] factories) : this()
        {
            this.Source = source;
            PropertyChangedEventManager.AddListener(source, this, string.Empty);
            this.Factories = factories;
            this.Dispatch(this.Run);
        }

        public MonitoringAsyncResult(TaskScheduler scheduler, IObservable source, params Func<Task<T>>[] factories) : this()
        {
            this.Scheduler = scheduler;
            this.Source = source;
            PropertyChangedEventManager.AddListener(source, this, string.Empty);
            this.Factories = factories;
            this.Tasks.Add(this.Scheduler.StartNew(this.Run));
        }

        public MonitoringAsyncResult(IObservable source, T value, params Func<Task<T>>[] factories) : this()
        {
            this.Source = source;
            PropertyChangedEventManager.AddListener(source, this, string.Empty);
            this.Value = value;
            this.Factories = factories;
            this.Dispatch(this.Run);
        }

        public MonitoringAsyncResult(TaskScheduler scheduler, IObservable source, T value, params Func<Task<T>>[] factories) : this()
        {
            this.Scheduler = scheduler;
            this.Source = source;
            PropertyChangedEventManager.AddListener(source, this, string.Empty);
            this.Value = value;
            this.Factories = factories;
            this.Tasks.Add(this.Scheduler.StartNew(this.Run));
        }

        public List<Task> Tasks { get; private set; }

        public TaskScheduler Scheduler { get; private set; }

        public IObservable Source { get; private set; }

        public Func<Task<T>>[] Factories { get; private set; }

        public async Task Run()
        {
            foreach (var factory in this.Factories)
            {
                var value = await factory().ConfigureAwait(false);
                await Windows.Invoke(() => this.Value = value).ConfigureAwait(false);
            }
        }

        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (e is PropertyChangedEventArgs propertyChangedEventArgs && string.IsNullOrEmpty(propertyChangedEventArgs.PropertyName))
            {
                this.Tasks.Add(this.Scheduler.StartNew(this.Run));
            }
            return true;
        }

        protected override void OnDisposing()
        {
            foreach (var task in this.Tasks)
            {
                this.Scheduler.Cancel(task);
            }
            PropertyChangedEventManager.RemoveListener(this.Source, this, string.Empty);
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new MonitoringAsyncResult<T>();
        }
    }
}
