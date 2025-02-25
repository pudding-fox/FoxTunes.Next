using FoxTunes.Interfaces;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace FoxTunes
{
    public static class PlaybackStateNotifier
    {
        private static readonly TimeSpan UPDATE_INTERVAL = TimeSpan.FromMilliseconds(500);

        public static readonly IOutput Output;

        public static readonly IPlaybackManager PlaybackManager;

        public static readonly IConfiguration Configuration;

        public static readonly DispatcherTimer Timer;

        private static bool _IsStarted { get; set; }

        public static bool IsStarted
        {
            get
            {
                return _IsStarted;
            }
            private set
            {
                _IsStarted = value;
                OnIsStartedChanged();
            }
        }

        private static void OnIsStartedChanged()
        {
            if (IsStartedChanged != null)
            {
                IsStartedChanged(typeof(PlaybackStateNotifier), EventArgs.Empty);
            }
        }

        public static event EventHandler IsStartedChanged;

        private static bool _IsPlaying { get; set; }

        public static bool IsPlaying
        {
            get
            {
                return _IsPlaying;
            }
            private set
            {
                _IsPlaying = value;
                OnIsPlayingChanged();
            }
        }

        private static void OnIsPlayingChanged()
        {
            if (IsPlayingChanged != null)
            {
                IsPlayingChanged(typeof(PlaybackStateNotifier), EventArgs.Empty);
            }
        }

        public static event EventHandler IsPlayingChanged;

        private static bool _IsPaused { get; set; }

        public static bool IsPaused
        {
            get
            {
                return _IsPaused;
            }
            private set
            {
                _IsPaused = value;
                OnIsPausedChanged();
            }
        }

        private static void OnIsPausedChanged()
        {
            if (IsPausedChanged != null)
            {
                IsPausedChanged(typeof(PlaybackStateNotifier), EventArgs.Empty);
            }
        }

        public static event EventHandler IsPausedChanged;

        static PlaybackStateNotifier()
        {
            Output = ComponentRegistry.Instance.GetComponent<IOutput>();
            PlaybackManager = ComponentRegistry.Instance.GetComponent<IPlaybackManager>();
            Configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();
            if (Output == null || PlaybackManager == null || Configuration == null)
            {
                return;
            }
            Output.IsStartedChanged += OnIsStartedChanged;
            PlaybackManager.CurrentStreamChanged += OnCurrentStreamChanged;
            Timer = new DispatcherTimer(DispatcherPriority.Background);
            Configuration.GetElement<IntegerConfigurationElement>(
                WindowsUserInterfaceConfiguration.SECTION,
                WindowsUserInterfaceConfiguration.TIMER_FREQUENCY
            ).ConnectValue(value => Timer.Interval = TimeSpan.FromMilliseconds(value));
            Timer.Start();
            Timer.Tick += OnTick;
        }

        private static void OnIsStartedChanged(object sender, AsyncEventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
#if NET40
            var task = TaskEx.Run(() => Windows.Invoke(() => Update()));
#else
            var task = Task.Run(() => Windows.Invoke(() => Update()));
#endif
        }

        private static void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
#if NET40
            var task = TaskEx.Run(() => Windows.Invoke(() => Update()));
#else
            var task = Task.Run(() => Windows.Invoke(() => Update()));
#endif
        }

        private static void OnTick(object sender, EventArgs e)
        {
            try
            {
                Update();
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }

        private static void Update()
        {
            var isStarted = Output.IsStarted;
            if (isStarted != IsStarted)
            {
                IsStarted = isStarted;
            }
            var isPlaying = default(bool);
            var isPaused = default(bool);
            var outputStream = PlaybackManager.CurrentStream;
            if (outputStream != null)
            {
                isPlaying = outputStream.IsPlaying;
                isPaused = outputStream.IsPaused;
            }
            if (isPlaying != IsPlaying)
            {
                IsPlaying = isPlaying;
            }
            if (isPaused != IsPaused)
            {
                IsPaused = isPaused;
            }
            OnNotify();
        }

        private static void OnNotify()
        {
            if (Notify != null)
            {
                Notify(typeof(PlaybackStateNotifier), EventArgs.Empty);
            }
        }

        public static event EventHandler Notify;
    }
}
