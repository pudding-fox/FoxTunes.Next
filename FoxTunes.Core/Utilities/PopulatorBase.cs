using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace FoxTunes
{
    public abstract class PopulatorBase : BaseComponent, IReportsProgress, IDisposable
    {
        public const int LONG_INTERVAL = 1000;

        public const int NORMAL_INTERVAL = 500;

        public const int FAST_INTERVAL = 100;

        public static readonly object SyncRoot = new object();

        private PopulatorBase()
        {
            this.Semaphore = new SemaphoreSlim(1, 1);
        }

        public PopulatorBase(bool reportProgress) : this()
        {
            this.ReportProgress = reportProgress;
            if (this.ReportProgress)
            {
                lock (SyncRoot)
                {
                    this.Timer = new global::System.Timers.Timer();
                    this.Timer.Interval = NORMAL_INTERVAL;
                    this.Timer.AutoReset = false;
                    this.Timer.Elapsed += this.OnElapsed;
                }
                this.CountMetric = new Metric(1000);
                this.SecondsMetric = new Metric(1000);
            }
        }

        protected virtual void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                lock (SyncRoot)
                {
                    if (this.Timer == null)
                    {
                        return;
                    }
                    this.Timer.Start();
                }
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }

        public SemaphoreSlim Semaphore { get; private set; }

        public bool ReportProgress { get; private set; }

        public global::System.Timers.Timer Timer { get; private set; }

        public Metric CountMetric { get; private set; }

        public Metric SecondsMetric { get; private set; }

        private string _Name { get; set; }

        public string Name
        {
            get
            {
                return this._Name;
            }
            protected set
            {
                this._Name = value;
                this.OnNameChanged();
            }
        }

        protected virtual void OnNameChanged()
        {
            if (this.NameChanged != null)
            {
                this.NameChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Name");
        }

        public event EventHandler NameChanged;

        private string _Description { get; set; }

        public string Description
        {
            get
            {
                return this._Description;
            }
            protected set
            {
                this._Description = value;
                this.OnDescriptionChanged();
            }
        }

        protected virtual void OnDescriptionChanged()
        {
            if (this.DescriptionChanged != null)
            {
                this.DescriptionChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Description");
        }

        public event EventHandler DescriptionChanged;

        private int _Position { get; set; }

        public int Position
        {
            get
            {
                return this._Position;
            }
            protected set
            {
                this._Position = value;
                this.OnPositionChanged();
            }
        }

        protected virtual void OnPositionChanged()
        {
            if (this.PositionChanged != null)
            {
                this.PositionChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Position");
        }

        public event EventHandler PositionChanged;

        private int _Count { get; set; }

        public int Count
        {
            get
            {
                return this._Count;
            }
            protected set
            {
                this._Count = value;
                this.OnCountChanged();
            }
        }

        protected virtual void OnCountChanged()
        {
            if (this.CountChanged != null)
            {
                this.CountChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Count");
        }

        public event EventHandler CountChanged;

        protected bool _IsIndeterminate { get; set; }

        public bool IsIndeterminate
        {
            get
            {
                return this._IsIndeterminate;
            }
            protected set
            {
                this._IsIndeterminate = value;
                this.OnIsIndeterminateChanged();
            }
        }

        protected virtual void OnIsIndeterminateChanged()
        {
            if (this.IsIndeterminateChanged != null)
            {
                this.IsIndeterminateChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("IsIndeterminate");
        }

        public event EventHandler IsIndeterminateChanged;

        public virtual ParallelOptions ParallelOptions
        {
            get
            {
                return new ParallelOptions()
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                };
            }
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            lock (SyncRoot)
            {
                if (this.Timer != null)
                {
                    this.Timer.Stop();
                    this.Timer.Elapsed -= this.OnElapsed;
                    this.Timer.Dispose();
                    this.Timer = null;
                }
            }
        }

        ~PopulatorBase()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        public string GetEta(int count)
        {
            var seconds = this.SecondsMetric.Next(
                (this.Count - this.Position) / this.CountMetric.Average(count)
            );
            if (seconds > 0)
            {
                var time = TimeSpan.FromSeconds(seconds);
                var elements = new List<string>();
                if (time.Hours > 0)
                {
                    elements.Add(string.Format("{0} hours", time.Hours));
                }
                if (time.Minutes > 0)
                {
                    elements.Add(string.Format("{0} minutes", time.Minutes));
                }
                if (time.Seconds > 0)
                {
                    elements.Add(string.Format("{0} seconds", time.Seconds));
                }
                return string.Join(", ", elements);
            }
            else
            {
                return "now";
            }
        }
    }
}
