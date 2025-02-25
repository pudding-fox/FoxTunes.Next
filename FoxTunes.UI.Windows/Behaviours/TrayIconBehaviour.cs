using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class TrayIconBehaviour : StandardBehaviour, IInvocableComponent, IDisposable
    {
        public const string QUIT = "ZZZZ";

        private static readonly Lazy<Icon> _Icon = new Lazy<Icon>(() =>
        {
            using (var stream = typeof(Main).Assembly.GetManifestResourceStream("FoxTunes.UI.Windows.Images.Fox.ico"))
            {
                if (stream == null)
                {
                    return null;
                }
                return new Icon(stream);
            }
        });

        public static Icon Icon
        {
            get
            {
                return _Icon.Value;
            }
        }

        private static readonly Lazy<Menu> _Menu = new Lazy<Menu>(() =>
        {
            return new Menu()
            {
                Category = InvocationComponent.CATEGORY_NOTIFY_ICON,
                Placement = PlacementMode.AbsolutePoint
            };
        });

        public static Menu Menu
        {
            get
            {
                return _Menu.Value;
            }
        }

        public Window Window { get; private set; }

        public INotifyIcon NotifyIcon { get; private set; }

        public IConfiguration Configuration { get; private set; }

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
            if (this.Enabled)
            {
                this.Enable();
            }
            else
            {
                this.Disable();
            }
        }

        public bool MinimizeToTray { get; private set; }

        public bool CloseToTray { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.NotifyIcon = ComponentRegistry.Instance.GetComponent<INotifyIcon>();
            this.Configuration = core.Components.Configuration;
            if (this.Configuration.GetSection(NotifyIconConfiguration.SECTION) != null)
            {
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    NotifyIconConfiguration.SECTION,
                    NotifyIconConfiguration.ENABLED_ELEMENT
                ).ConnectValue(value => this.Enabled = value);
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    NotifyIconConfiguration.SECTION,
                    NotifyIconConfiguration.MINIMIZE_TO_TRAY_ELEMENT
                ).ConnectValue(value => this.MinimizeToTray = value);
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    NotifyIconConfiguration.SECTION,
                    NotifyIconConfiguration.CLOSE_TO_TRAY_ELEMENT
                ).ConnectValue(value => this.CloseToTray = value);
            }
            base.InitializeComponent(core);
        }

        protected virtual void AddWindowHooks()
        {
            var ids = Windows.Registrations.IdsByRole(UserInterfaceWindowRole.Main);
            Windows.Registrations.AddCreated(ids, this.OnWindowCreated);
            Windows.Registrations.AddClosed(ids, this.OnWindowClosed);
            foreach (var window in Windows.Registrations.WindowsByIds(ids))
            {
                this.AddWindowHooks(window);
            }
        }

        protected virtual void AddWindowHooks(Window window)
        {
            window.StateChanged += this.OnStateChanged;
            window.Closing += this.OnClosing;
            Logger.Write(this, LogLevel.Debug, "Registered window events: {0}/{1}", window.GetType().Name, window.Title);
        }

        protected virtual void RemoveWindowHooks()
        {
            var ids = Windows.Registrations.IdsByRole(UserInterfaceWindowRole.Main);
            Windows.Registrations.RemoveCreated(ids, this.OnWindowCreated);
            Windows.Registrations.RemoveClosed(ids, this.OnWindowClosed);
            foreach (var window in Windows.Registrations.WindowsByIds(ids))
            {
                this.RemoveWindowHooks(window);
            }
        }

        protected virtual void RemoveWindowHooks(Window window)
        {
            window.StateChanged -= this.OnStateChanged;
            window.Closing -= this.OnClosing;
            Logger.Write(this, LogLevel.Debug, "Unregistered window events: {0}/{1}", window.GetType().Name, window.Title);
        }

        protected virtual void OnWindowCreated(object sender, EventArgs e)
        {
            if (sender is Window window)
            {
                this.AddWindowHooks(window);
            }
        }

        protected virtual void OnWindowClosed(object sender, EventArgs e)
        {
            if (sender is Window window)
            {
                this.RemoveWindowHooks(window);
            }
        }

        protected virtual void Enable()
        {
            Windows.ShuttingDown += this.OnShuttingDown;
            this.NotifyIcon.Icon = Icon.Handle;
            this.NotifyIcon.Show();
            if (this.NotifyIcon.MessageSink != null)
            {
                this.NotifyIcon.MessageSink.MouseLeftButtonUp += this.OnMouseLeftButtonUp;
                this.NotifyIcon.MessageSink.MouseRightButtonUp += this.OnMouseRightButtonUp;
                Logger.Write(this, LogLevel.Debug, "Registered message sink events.");
            }
            this.AddWindowHooks();
        }

        protected virtual void Disable()
        {
            Windows.ShuttingDown -= this.OnShuttingDown;
            if (this.NotifyIcon != null)
            {
                this.NotifyIcon.Hide();
                if (this.NotifyIcon.MessageSink != null)
                {
                    this.NotifyIcon.MessageSink.MouseLeftButtonUp -= this.OnMouseLeftButtonUp;
                    this.NotifyIcon.MessageSink.MouseRightButtonUp -= this.OnMouseRightButtonUp;
                    Logger.Write(this, LogLevel.Debug, "Unregistered message sink events.");
                }
            }
            this.RemoveWindowHooks();
        }

        protected virtual void OnShuttingDown(object sender, EventArgs e)
        {
            Logger.Write(this, LogLevel.Debug, "Shutdown signal recieved.");
            this.Disable();
        }

        protected virtual void OnMouseLeftButtonUp(object sender, EventArgs e)
        {
            var window = this.Window ?? Windows.ActiveWindow;
            if (window == null)
            {
                Logger.Write(this, LogLevel.Warn, "No window to restore.");
                return;
            }
            this.Window = null;
            var task = Windows.Invoke(() =>
            {
                Logger.Write(this, LogLevel.Debug, "Restoring window: {0}/{1}", window.GetType().Name, window.Title);
                window.Show();
                if (window.WindowState == WindowState.Minimized)
                {
                    window.WindowState = WindowState.Normal;
                }
                window.BringToFront();
            });
        }

        protected virtual void OnMouseRightButtonUp(object sender, EventArgs e)
        {
            var task = Windows.Invoke(() =>
            {
                var x = default(int);
                var y = default(int);
                MouseHelper.GetPosition(out x, out y);
                DpiHelper.TransformPosition(ref x, ref y);

                Menu.HorizontalOffset = x;
                Menu.VerticalOffset = y;
                Menu.IsOpen = true;

                var source = PresentationSource.FromVisual(Menu) as HwndSource;
                if (source != null && source.Handle != IntPtr.Zero)
                {
                    SetForegroundWindow(source.Handle);
                }
            });
        }

        protected virtual void OnClose(object sender, EventArgs e)
        {
            this.Disable();
            var task = this.Quit();
        }

        protected virtual Task Quit()
        {
            return Windows.Shutdown();
        }

        protected virtual void OnStateChanged(object sender, EventArgs e)
        {
            if (this.MinimizeToTray)
            {
                if (sender is Window window)
                {
                    if (window.WindowState == WindowState.Minimized)
                    {
                        Logger.Write(this, LogLevel.Debug, "MinimizeToTray: Hiding window: {0}/{1}", window.GetType().Name, window.Title);
                        window.Hide();
                        this.Window = window;
                        return;
                    }
                }
            }
            this.Window = null;
        }

        protected virtual void OnClosing(object sender, CancelEventArgs e)
        {
            if (this.CloseToTray)
            {
                if (sender is Window window)
                {
                    e.Cancel = true;
                    Logger.Write(this, LogLevel.Debug, "CloseToTray: Hiding window: {0}/{1}", window.GetType().Name, window.Title);
                    window.Hide();
                    this.Window = window;
                    return;
                }
            }
            this.Window = null;
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return InvocationComponent.CATEGORY_NOTIFY_ICON;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                if (this.Enabled)
                {
                    yield return new InvocationComponent(InvocationComponent.CATEGORY_NOTIFY_ICON, QUIT, "Quit");
                }
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case QUIT:
                    return Windows.Invoke(() =>
                    {
                        this.Disable();
                        this.OnClose(this, EventArgs.Empty);
                    });
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
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
            this.Disable();
        }

        ~TrayIconBehaviour()
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

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);
    }
}
