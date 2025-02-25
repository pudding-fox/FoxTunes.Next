using FoxTunes.Interfaces;
using FoxTunes.ViewModel;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for SearchBox.xaml
    /// </summary>
    public partial class SearchBox : UserControl
    {
        const int DEFAULT_INTERVAL = 1000;

        public static readonly ISignalEmitter SignalEmitter = ComponentRegistry.Instance.GetComponent<ISignalEmitter>();

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text",
            typeof(string),
            typeof(SearchBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnTextChanged))
        );

        public static string GetText(SearchBox source)
        {
            return (string)source.GetValue(TextProperty);
        }

        public static void SetText(SearchBox source, string value)
        {
            source.SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var searchBox = sender as SearchBox;
            if (searchBox == null)
            {
                return;
            }
            searchBox.OnTextChanged();
        }

        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(
            "SearchText",
            typeof(string),
            typeof(SearchBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnSearchTextChanged))
        );

        public static string GetSearchText(SearchBox source)
        {
            return (string)source.GetValue(SearchTextProperty);
        }

        public static void SetSearchText(SearchBox source, string value)
        {
            source.SetValue(SearchTextProperty, value);
        }

        private static void OnSearchTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var searchBox = sender as SearchBox;
            if (searchBox == null)
            {
                return;
            }
            searchBox.OnSearchTextChanged();
        }

        public static readonly DependencyProperty IntervalProperty = DependencyProperty.Register(
            "Interval",
            typeof(int),
            typeof(SearchBox),
            new FrameworkPropertyMetadata(DEFAULT_INTERVAL, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, new PropertyChangedCallback(OnIntervalChanged))
        );

        public static int GetInterval(SearchBox source)
        {
            return (int)source.GetValue(IntervalProperty);
        }

        public static void SetInterval(SearchBox source, int value)
        {
            source.SetValue(IntervalProperty, value);
        }

        private static void OnIntervalChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var searchBox = sender as SearchBox;
            if (searchBox == null)
            {
                return;
            }
            searchBox.OnIntervalChanged();
        }

        public static readonly RoutedEvent CommitEvent = EventManager.RegisterRoutedEvent(
            "Commit",
            RoutingStrategy.Bubble,
            typeof(RoutedEventHandler),
            typeof(SearchBox)
        );

        public SearchBox()
        {
            this.InitializeComponent();
        }

        private DispatcherTimer Timer { get; set; }

        private bool IsCommitting { get; set; }

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
            if (this.Timer != null)
            {
                this.Timer.Stop();
            }
            else
            {
                this.Timer = new DispatcherTimer();
                this.Timer.Interval = TimeSpan.FromMilliseconds(this.Interval);
                this.Timer.Tick += this.OnTimerTick;
            }
            this.Timer.Start();
        }

        public string SearchText
        {
            get
            {
                return GetSearchText(this);
            }
            set
            {
                SetSearchText(this, value);
            }
        }

        protected virtual void OnSearchTextChanged()
        {
            if (!string.Equals(this.Text, this.SearchText, StringComparison.OrdinalIgnoreCase))
            {
                this.Text = this.SearchText;
            }
        }

        public int Interval
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
            if (this.Timer != null)
            {
                this.Timer.Interval = TimeSpan.FromMilliseconds(this.Interval);
            }
        }

        public event RoutedEventHandler Commit
        {
            add
            {
                this.AddHandler(CommitEvent, value);
            }
            remove
            {
                this.RemoveHandler(CommitEvent, value);
            }
        }

        protected virtual void OnTimerTick(object sender, EventArgs e)
        {
            if (this.Timer != null)
            {
                this.Timer.Stop();
            }
            this.SearchText = this.Text;
            if (this.IsCommitting)
            {
                this.IsCommitting = false;
                this.OnCommit();
            }
        }

        public ICommand ClearCommand
        {
            get
            {
                return new Command(() => this.Clear());
            }
        }

        public void Clear()
        {
            if (this.Timer != null)
            {
                this.Timer.Stop();
            }
            this.Text = string.Empty;
            this.SearchText = string.Empty;
        }

        protected virtual void OnKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Return:
                    this.IsCommitting = true;
                    this.OnTimerTick(this.Timer, EventArgs.Empty);
                    break;
            }
        }

        protected virtual void OnCommit()
        {
            this.RaiseEvent(new RoutedEventArgs(CommitEvent));
        }

        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (SignalEmitter != null)
            {
                SignalEmitter.Signal += this.OnSignal;
            }
        }

        protected virtual void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (SignalEmitter != null)
            {
                SignalEmitter.Signal -= this.OnSignal;
            }
        }

        protected virtual Task OnSignal(object sender, ISignal signal)
        {
            //TODO: This is a hack. The KeyBindingsBehaviour sends this signal. Nothing else uses this pattern.
            if (this.IsVisible)
            {
                switch (signal.Name)
                {
                    case CommonSignals.PluginInvocation:
                        if (signal.State is PluginInvocationSignalState state)
                        {
                            switch (state.Id)
                            {
                                case DefaultKeyBindingsBehaviour.SEARCH:
                                    return Windows.Invoke(() => this.TextBox.Focus());
                            }
                        }
                        break;
                }
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }
    }
}
