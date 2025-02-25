using FoxTunes.Interfaces;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace FoxTunes.ViewModel
{
    public class Visualization : ViewModelBase
    {
        public Visualization()
        {
#if DEBUG
            this.Timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Render, this.OnCalculateFPS, this.Dispatcher);
#endif
        }

#if DEBUG

        public DispatcherTimer Timer { get; private set; }

        public volatile int Frames;

        protected virtual void OnCalculateFPS(object sender, EventArgs e)
        {
            this.FPS = Interlocked.Exchange(ref this.Frames, 0);
        }
#endif

        public IOutput Output { get; private set; }

        public IOutputDataSource OutputDataSource { get; private set; }

        private string _StatusMessage { get; set; }

        public string StatusMessage
        {
            get
            {
                return this._StatusMessage;
            }
            set
            {
                this._StatusMessage = value;
                this.OnStatusMessageChanged();
            }
        }

        protected virtual void OnStatusMessageChanged()
        {
            if (this.StatusMessageChanged != null)
            {
                this.StatusMessageChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("StatusMessage");
        }

        public event EventHandler StatusMessageChanged;

        private string _StatusMessageDetail { get; set; }

        public string StatusMessageDetail
        {
            get
            {
                return this._StatusMessageDetail;
            }
            set
            {
                this._StatusMessageDetail = value;
                this.OnStatusMessageDetailChanged();
            }
        }

        protected virtual void OnStatusMessageDetailChanged()
        {
            if (this.StatusMessageDetailChanged != null)
            {
                this.StatusMessageDetailChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("StatusMessageDetail");
        }

        public event EventHandler StatusMessageDetailChanged;

        private bool _HasStatusMessage { get; set; }

        public bool HasStatusMessage
        {
            get
            {
                return this._HasStatusMessage;
            }
            set
            {
                this._HasStatusMessage = value;
                this.OnHasStatusMessageChanged();
            }
        }

        protected virtual void OnHasStatusMessageChanged()
        {
            if (this.HasStatusMessageChanged != null)
            {
                this.HasStatusMessageChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("HasStatusMessage");
        }

        public event EventHandler HasStatusMessageChanged;

        public bool ShowFPS
        {
            get
            {
                //TODO: Make this configurable.
#if DEBUG
                return true;
#else
                return false;
#endif
            }
        }

        protected virtual void OnShowFPSChanged()
        {
            if (this.ShowFPSChanged != null)
            {
                this.ShowFPSChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ShowFPS");
        }

        public event EventHandler ShowFPSChanged;

        private int _FPS { get; set; }

        public int FPS
        {
            get
            {
                return this._FPS;
            }
            set
            {
                this._FPS = value;
                this.OnFPSChanged();
            }
        }

        protected virtual void OnFPSChanged()
        {
            if (this.FPSChanged != null)
            {
                this.FPSChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("FPS");
        }

        public event EventHandler FPSChanged;

        protected override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output;
            this.Output.IsStartedChanged += this.OnIsStartedChanged;
            this.OutputDataSource = core.Components.OutputDataSource;
            this.OutputDataSource.CanGetDataChanged += this.OnCanGetDataChanged;
            this.Update();
            base.InitializeComponent(core);
        }

        protected virtual void OnIsStartedChanged(object sender, AsyncEventArgs e)
        {
            var task = Windows.Invoke(this.Update);
        }

        protected virtual void OnCanGetDataChanged(object sender, EventArgs e)
        {
            var task = Windows.Invoke(this.Update);
        }

        protected virtual void Update()
        {
            if (this.Output.IsStarted && !this.OutputDataSource.CanGetData)
            {
                this.StatusMessage = Strings.Visualization_Unavailable;
                this.StatusMessageDetail = Strings.Visualization_UnavailableDetail;
                this.HasStatusMessage = true;
            }
            else
            {
                this.StatusMessage = null;
                this.StatusMessageDetail = null;
                this.HasStatusMessage = false;
            }
        }

        protected override Freezable CreateInstanceCore()
        {
            return new Visualization();
        }

        protected override void OnDisposing()
        {
            if (this.Output != null)
            {
                this.Output.IsStartedChanged -= this.OnIsStartedChanged;
            }
            if (this.OutputDataSource != null)
            {
                this.OutputDataSource.CanGetDataChanged -= this.OnCanGetDataChanged;
            }
            base.OnDisposing();
        }
    }
}
