﻿using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace FoxTunes.ViewModel
{
    public class StreamPosition : ViewModelBase
    {
        public IPlaybackManager PlaybackManager { get; private set; }

        public IConfiguration Configuration { get; private set; }

        private BooleanConfigurationElement _ShowCounters { get; set; }

        public BooleanConfigurationElement ShowCounters
        {
            get
            {
                return this._ShowCounters;
            }
            set
            {
                this._ShowCounters = value;
                this.OnShowCountersChanged();
            }
        }

        protected virtual void OnShowCountersChanged()
        {
            if (this.ShowCountersChanged != null)
            {
                this.ShowCountersChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowCounters");
        }

        public event EventHandler ShowCountersChanged;

        private OutputStream _CurrentStream { get; set; }

        public OutputStream CurrentStream
        {
            get
            {
                return this._CurrentStream;
            }
            private set
            {
                this._CurrentStream = value;
                this.OnCurrentStreamChanged();
            }
        }

        protected virtual void OnCurrentStreamChanged()
        {
            if (this.CurrentStreamChanged != null)
            {
                this.CurrentStreamChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("CurrentStream");
        }

        public event EventHandler CurrentStreamChanged;

        public ICommand BeginSeekCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand<MouseButtonEventArgs>(this.BeginSeek);
            }
        }

        public Task BeginSeek(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && this.CurrentStream != null)
            {
                return this.CurrentStream.BeginSeek();
            }
            else
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
        }

        public ICommand EndSeekCommand
        {
            get
            {
                return CommandFactory.Instance.CreateCommand<MouseButtonEventArgs>(this.EndSeek);
            }
        }

        public Task EndSeek(MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && this.CurrentStream != null)
            {
                return this.CurrentStream.EndSeek();
            }
            else
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
        }

        protected override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.Configuration = core.Components.Configuration;
            this.ShowCounters = this.Configuration.GetElement<BooleanConfigurationElement>(
                StreamPositionBehaviourConfiguration.SECTION,
                StreamPositionBehaviourConfiguration.SHOW_COUNTERS_ELEMENT
            );
            this.Dispatch(this.Refresh);
            base.InitializeComponent(core);
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        public Task Refresh()
        {
            return Windows.Invoke(() =>
            {
                if (this.PlaybackManager.CurrentStream != null)
                {
                    if (this.CurrentStream != null)
                    {
                        this.CurrentStream.Dispose();
                    }
                    this.CurrentStream = new OutputStream(this.PlaybackManager.CurrentStream);
                }
                else
                {
                    this.CurrentStream = null;
                }
            });
        }

        protected override void OnDisposing()
        {
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new StreamPosition();
        }
    }
}
