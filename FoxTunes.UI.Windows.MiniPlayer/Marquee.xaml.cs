using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for Marquee.xaml
    /// </summary>
    public partial class Marquee : UserControl
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text",
            typeof(string),
            typeof(Marquee),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender, new PropertyChangedCallback(OnTextChanged))
        );

        public static string GetText(Marquee source)
        {
            return (string)source.GetValue(TextProperty);
        }

        public static void SetText(Marquee source, string value)
        {
            source.SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var marquee = sender as Marquee;
            if (marquee == null)
            {
                return;
            }
            marquee.OnTextChanged();
        }

        public static readonly DependencyProperty StepProperty = DependencyProperty.Register(
            "Step",
            typeof(double),
            typeof(Marquee),
            new FrameworkPropertyMetadata(0.75d, FrameworkPropertyMetadataOptions.None, new PropertyChangedCallback(OnStepChanged))
        );

        public static double GetStep(Marquee source)
        {
            return (double)source.GetValue(StepProperty);
        }

        public static void SetStep(Marquee source, double value)
        {
            source.SetValue(StepProperty, value);
        }

        private static void OnStepChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var marquee = sender as Marquee;
            if (marquee == null)
            {
                return;
            }
            marquee.OnStepChanged();
        }

        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
            "Interval",
            typeof(TimeSpan),
            typeof(Marquee),
            new FrameworkPropertyMetadata(TimeSpan.FromMilliseconds(50), FrameworkPropertyMetadataOptions.None, new PropertyChangedCallback(OnIntervalChanged))
        );

        public static TimeSpan GetInterval(Marquee source)
        {
            return (TimeSpan)source.GetValue(IntervalProperty);
        }

        public static void SetInterval(Marquee source, TimeSpan value)
        {
            source.SetValue(IntervalProperty, value);
        }

        private static void OnIntervalChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var marquee = sender as Marquee;
            if (marquee == null)
            {
                return;
            }
            marquee.OnIntervalChanged();
        }

        public static readonly DependencyProperty PauseProperty = DependencyProperty.Register(
            "Pause",
            typeof(TimeSpan),
            typeof(Marquee),
            new FrameworkPropertyMetadata(TimeSpan.FromSeconds(1), FrameworkPropertyMetadataOptions.None, new PropertyChangedCallback(OnPauseChanged))
        );

        public static TimeSpan GetPause(Marquee source)
        {
            return (TimeSpan)source.GetValue(PauseProperty);
        }

        public static void SetPause(Marquee source, TimeSpan value)
        {
            source.SetValue(PauseProperty, value);
        }

        private static void OnPauseChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var marquee = sender as Marquee;
            if (marquee == null)
            {
                return;
            }
            marquee.OnPauseChanged();
        }

        public Marquee()
        {
            this.Timer = new DispatcherTimer(DispatcherPriority.Normal, this.Dispatcher);
            this.Timer.Interval = TimeSpan.FromMilliseconds(100);
            this.Timer.Tick += this.OnTick;
            this.InitializeComponent();
        }

        public FlowDirection Direction { get; private set; }

        public DispatcherTimer Timer { get; private set; }

        public string Text
        {
            get
            {
                return GetText(this);
            }
            set
            {
                SetText(this, value);
            }
        }

        protected virtual void OnTextChanged()
        {
            //Nothing to do.
        }

        public double Step
        {
            get
            {
                return GetStep(this);
            }
            set
            {
                SetStep(this, value);
            }
        }

        protected virtual void OnStepChanged()
        {
            //Nothing to do.
        }

        public TimeSpan Interval
        {
            get
            {
                return GetInterval(this);
            }
            set
            {
                SetInterval(this, value);
            }
        }

        protected virtual void OnIntervalChanged()
        {
            this.Timer.Interval = this.Interval;
        }

        public TimeSpan Pause
        {
            get
            {
                return GetPause(this);
            }
            set
            {
                SetPause(this, value);
            }
        }

        protected virtual void OnPauseChanged()
        {
            //Nothing to do.
        }

        public double Position
        {
            get
            {
                return (double)this.TextBlock.GetValue(Canvas.LeftProperty);
            }
            set
            {
                this.TextBlock.SetValue(Canvas.LeftProperty, value);
            }
        }

        protected virtual void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.Reset();
            if (this.TextBlock.ActualWidth > this.ActualWidth)
            {
                this.Start();
            }
            else
            {
                this.Stop();
            }
        }

        protected virtual void Start()
        {
            this.Reset();
#if NET40
            this.Dispatcher.BeginInvoke(new Func<Task>(async () =>
            {
                await TaskEx.Delay(this.Pause).ConfigureAwait(false);
                this.Timer.Start();
            }));
#else
            this.Dispatcher.InvokeAsync(async () =>
            {
                await Task.Delay(this.Pause).ConfigureAwait(false);
                this.Timer.Start();
            });
#endif
        }

        protected virtual void Stop()
        {
            this.Timer.Stop();
            this.Reset();
        }

        protected virtual void Reset()
        {
            this.Direction = FlowDirection.RightToLeft;
            if (this.TextBlock.ActualWidth > this.ActualWidth)
            {
                this.Position = 0;
            }
            else
            {
                this.Position = (this.ActualWidth - this.TextBlock.ActualWidth) / 2;
            }
        }

        protected virtual void Reverse()
        {
            if (this.Timer.IsEnabled)
            {
                this.Timer.Stop();
#if NET40
                this.Dispatcher.BeginInvoke(new Func<Task>(async () =>
                {
                    await TaskEx.Delay(this.Pause).ConfigureAwait(false);
                    this.Timer.Start();
                }));
#else
                this.Dispatcher.InvokeAsync(async () =>
                {
                    await Task.Delay(this.Pause).ConfigureAwait(false);
                    this.Timer.Start();
                });
#endif
            }
            switch (this.Direction)
            {
                case FlowDirection.LeftToRight:
                    this.Direction = FlowDirection.RightToLeft;
                    break;
                case FlowDirection.RightToLeft:
                    this.Direction = FlowDirection.LeftToRight;
                    break;
            }
        }

        protected virtual void Update()
        {
            var maximum = this.ActualWidth - this.TextBlock.ActualWidth;
            if (maximum >= 0)
            {
                this.Stop();
                return;
            }
            switch (this.Direction)
            {
                case FlowDirection.LeftToRight:
                    if (this.Position < 0)
                    {
                        this.Position += this.Step;
                    }
                    else
                    {
                        this.Reverse();
                    }
                    break;
                case FlowDirection.RightToLeft:
                    if (this.Position > maximum)
                    {
                        this.Position -= this.Step;
                    }
                    else
                    {
                        this.Reverse();
                    }
                    break;
            }
        }

        protected virtual void OnTick(object sender, EventArgs e)
        {
            try
            {
                this.Update();
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }
    }
}