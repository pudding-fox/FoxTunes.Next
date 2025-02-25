using FoxTunes.Interfaces;
using System;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class BackgroundTask : ViewModelBase
    {
        public BackgroundTask(IBackgroundTask backgroundTask)
        {
            if (backgroundTask != null)
            {
                this.InnerBackgroundTask = backgroundTask;
                this.InnerBackgroundTask.NameChanged += this.OnNameChanged;
                this.InnerBackgroundTask.DescriptionChanged += this.OnDescriptionChanged;
                this.InnerBackgroundTask.PositionChanged += this.OnPositionChanged;
                this.InnerBackgroundTask.CountChanged += this.OnCountChanged;
            }
        }

        public IBackgroundTask InnerBackgroundTask { get; private set; }

        public string Name
        {
            get
            {
                return this.InnerBackgroundTask.Name;
            }
        }

        protected virtual async void OnNameChanged(object sender, EventArgs e)
        {
            await Windows.Invoke(() =>
            {
                if (this.NameChanged != null)
                {
                    this.NameChanged(sender, e);
                }
                this.OnPropertyChanged("Name");
            }).ConfigureAwait(false);
        }

        public event EventHandler NameChanged;

        public string Description
        {
            get
            {
                return this.InnerBackgroundTask.Description;
            }
        }

        protected virtual async void OnDescriptionChanged(object sender, EventArgs e)
        {
            await Windows.Invoke(() =>
            {
                if (this.DescriptionChanged != null)
                {
                    this.DescriptionChanged(sender, e);
                }
                this.OnPropertyChanged("Description");
            }).ConfigureAwait(false);
        }

        public event EventHandler DescriptionChanged;

        public int Position
        {
            get
            {
                return this.InnerBackgroundTask.Position;
            }
        }

        protected virtual async void OnPositionChanged(object sender, EventArgs e)
        {
            await Windows.Invoke(() =>
            {
                if (this.PositionChanged != null)
                {
                    this.PositionChanged(sender, e);
                }
                this.OnPropertyChanged("Position");
            }).ConfigureAwait(false);
        }

        public event EventHandler PositionChanged;

        public int Count
        {
            get
            {
                return this.InnerBackgroundTask.Count;
            }
        }

        protected virtual async void OnCountChanged(object sender, EventArgs e)
        {
            await Windows.Invoke(() =>
            {
                this.OnIsIndeterminateChanged(sender, e);
                if (this.CountChanged != null)
                {
                    this.CountChanged(sender, e);
                }
                this.OnPropertyChanged("Count");
            }).ConfigureAwait(false);
        }

        public event EventHandler CountChanged;

        public bool IsIndeterminate
        {
            get
            {
                return this.Count == 0;
            }
        }

        protected virtual async void OnIsIndeterminateChanged(object sender, EventArgs e)
        {
            await Windows.Invoke(() =>
            {
                if (this.IsIndeterminateChanged != null)
                {
                    this.IsIndeterminateChanged(sender, e);
                }
                this.OnPropertyChanged("IsIndeterminate");
            }).ConfigureAwait(false);
        }

        public event EventHandler IsIndeterminateChanged;

        public ICommand CancelCommand
        {
            get
            {
                return new Command(this.Cancel, () => this.CanCancel);
            }
        }

        public bool CanCancel
        {
            get
            {
                return this.InnerBackgroundTask.Cancellable;
            }
        }

        public void Cancel()
        {
            this.InnerBackgroundTask.Cancel();
        }

        protected override void OnDisposing()
        {
            this.InnerBackgroundTask.NameChanged -= this.OnNameChanged;
            this.InnerBackgroundTask.DescriptionChanged -= this.OnDescriptionChanged;
            this.InnerBackgroundTask.PositionChanged -= this.OnPositionChanged;
            this.InnerBackgroundTask.CountChanged -= this.OnCountChanged;
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new BackgroundTask(null);
        }
    }
}
