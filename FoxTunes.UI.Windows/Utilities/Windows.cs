using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace FoxTunes
{
    public static class Windows
    {
        private static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static bool IsShuttingDown { get; set; }

        public static readonly WindowRegistrations Registrations = new WindowRegistrations();

        public static Dispatcher Dispatcher
        {
            get
            {
                if (ActiveWindow != null)
                {
                    return ActiveWindow.Dispatcher;
                }
                if (Application.Current != null)
                {
                    return Application.Current.Dispatcher;
                }
                if (!IsShuttingDown)
                {
                    //TODO: Questionable.
                    return Dispatcher.CurrentDispatcher;
                }
                return null;
            }
        }

        public static Window ActiveWindow
        {
            get
            {
                var windows = Registrations.WindowsByRole(UserInterfaceWindowRole.Main);
                foreach (var window in windows)
                {
                    if (window.IsVisible)
                    {
                        return window;
                    }
                }
                return windows.FirstOrDefault();
            }
        }

        private static void OnActiveWindowChanged()
        {
            if (ActiveWindowChanged == null)
            {
                return;
            }
            ActiveWindowChanged(ActiveWindow, EventArgs.Empty);
        }

        public static event EventHandler ActiveWindowChanged;

        public static Task TryShutdown()
        {
            Logger.Write(typeof(Windows), LogLevel.Debug, "Looking for main window..");
            return Invoke(() =>
            {
                var window = ActiveWindow;
                if (window != null && window.IsVisible)
                {
                    Logger.Write(typeof(Windows), LogLevel.Debug, "Main window is visible, nothing to do: {0}/{1}", window.GetType().Name, window.Title);
#if NET40
                    return TaskEx.FromResult(false);
#else
                    return Task.CompletedTask;
#endif
                }
                Logger.Write(typeof(Windows), LogLevel.Debug, "No visible windows, shutting down..");
                return Shutdown();
            });
        }

        public static Task Shutdown()
        {
            Logger.Write(typeof(Windows), LogLevel.Debug, "Shutting down..");
            IsShuttingDown = true;
            return Invoke(() =>
            {
                if (ShuttingDown != null)
                {
                    ShuttingDown(typeof(Windows), EventArgs.Empty);
                }
                foreach (var window in Registrations.Windows)
                {
                    Logger.Write(typeof(Windows), LogLevel.Debug, "Closing window: {0}/{1}", window.GetType().Name, window.Title);
                    window.Close();
                }
                foreach (var window in WindowBase.Active)
                {
                    Logger.Write(typeof(Windows), LogLevel.Debug, "Closing window: {0}/{1}", window.GetType().Name, window.Title);
                    window.Close();
                }
                UIBehaviour.Shutdown();
            });
        }

        public static event EventHandler ShuttingDown;

        public static Task Invoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var dispatcher = Dispatcher;
            if (dispatcher != null)
            {
                if (dispatcher.CheckAccess())
                {
                    action();
                }
                else
                {
#if NET40
                    var source = new TaskCompletionSource<bool>();
                    dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            action();
                        }
                        finally
                        {
                            source.SetResult(false);
                        }
                    }), priority);
                    return source.Task;
#else
                    return dispatcher.BeginInvoke(action).Task;
#endif
                }
            }
            else
            {
                Logger.Write(typeof(Windows), LogLevel.Warn, "Cannot Invoke, Dispatcher is not available.");
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public static Task Invoke(Func<Task> func)
        {
            var dispatcher = Dispatcher;
            if (dispatcher != null)
            {
                if (dispatcher.CheckAccess())
                {
                    return func();
                }
                else
                {
#if NET40
                    var source = new TaskCompletionSource<bool>();
                    dispatcher.BeginInvoke(new Action(async () =>
                    {
                        try
                        {
                            await func().ConfigureAwait(false);
                        }
                        finally
                        {
                            source.SetResult(false);
                        }
                    }));
                    return source.Task;
#else
                    return dispatcher.BeginInvoke(func).Task;
#endif
                }
            }
            else
            {
                Logger.Write(typeof(Windows), LogLevel.Warn, "Cannot Invoke, Dispatcher is not available.");
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public class WindowRegistrations : BaseComponent
        {
            public WindowRegistrations()
            {
                this.Store = new Dictionary<string, WindowRegistration>(StringComparer.OrdinalIgnoreCase);
                this.Callbacks = new Dictionary<string, ISet<EventHandler>>(StringComparer.OrdinalIgnoreCase);
            }

            public IDictionary<string, WindowRegistration> Store { get; private set; }

            public IDictionary<string, ISet<EventHandler>> Callbacks { get; private set; }

            public IEnumerable<Window> Windows
            {
                get
                {
                    foreach (var pair in this.Store)
                    {
                        var window = default(Window);
                        if (pair.Value.TryGetInstance(out window))
                        {
                            yield return window;
                        }
                    }
                }
            }

            public IEnumerable<string> IdsByRole(UserInterfaceWindowRole role)
            {
                return this.Store.Where(pair => pair.Value.Role == role).Select(pair => pair.Key);
            }

            public IEnumerable<WindowRegistration> RegistrationsByIds(IEnumerable<string> ids)
            {
                return this.Store.Where(pair => ids.Contains(pair.Value.Id, StringComparer.OrdinalIgnoreCase)).Select(pair => pair.Value);
            }

            public IEnumerable<WindowRegistration> RegistrationsByRole(UserInterfaceWindowRole role)
            {
                return this.Store.Where(pair => pair.Value.Role == role).Select(pair => pair.Value);
            }

            public IEnumerable<Window> WindowsByIds(IEnumerable<string> ids)
            {
                foreach (var pair in this.Store.Where(pair => ids.Contains(pair.Value.Id, StringComparer.OrdinalIgnoreCase)))
                {
                    var window = default(Window);
                    if (pair.Value.TryGetInstance(out window))
                    {
                        yield return window;
                    }
                }
            }

            public IEnumerable<Window> WindowsByRole(UserInterfaceWindowRole role)
            {
                foreach (var pair in this.Store.Where(pair => pair.Value.Role == role))
                {
                    var window = default(Window);
                    if (pair.Value.TryGetInstance(out window))
                    {
                        yield return window;
                    }
                }
            }

            public bool Add(WindowRegistration registration)
            {
                if (this.Store.TryAdd(registration.Id, registration))
                {
                    this.OnAdded(registration);
                    return true;
                }
                return false;
            }

            protected virtual void OnAdded(WindowRegistration registration)
            {
                Logger.Write(this, LogLevel.Debug, "Window registered: {0}", registration.Id);
                var callbacks = default(ISet<EventHandler>);
                if (this.Callbacks.TryGetValue(registration.Id, out callbacks))
                {
                    foreach (var callback in callbacks.ToArray())
                    {
                        callback(registration, EventArgs.Empty);
                    }
                }
            }

            public bool TryGet(string id, out WindowRegistration registration)
            {
                return this.Store.TryGetValue(id, out registration);
            }

            public bool TryGet(string id, out Window window)
            {
                var registration = default(WindowRegistration);
                if (!this.TryGet(id, out registration))
                {
                    window = default(Window);
                    return false;
                }
                return registration.TryGetInstance(out window);
            }

            public bool IsVisible(string id)
            {
                var window = default(Window);
                if (!this.TryGet(id, out window))
                {
                    return false;
                }
                return window.IsVisible;
            }

            public Window Show(string id)
            {
                var registration = default(WindowRegistration);
                if (!this.TryGet(id, out registration))
                {
                    return default(Window);
                }
                Logger.Write(this, LogLevel.Debug, "Showing window: {0}", id);
                var window = registration.GetInstance();
                if (!window.IsVisible)
                {
                    window.Show();
                }
                window.BringToFront();
                window.Activate();
                return window;
            }

            public bool Hide(string id)
            {
                var window = default(Window);
                if (!this.TryGet(id, out window))
                {
                    return false;
                }
                Logger.Write(this, LogLevel.Debug, "Hiding window: {0}", id);
                window.Hide();
                return true;
            }

            public bool Close(string id)
            {
                var window = default(Window);
                if (!this.TryGet(id, out window))
                {
                    return false;
                }
                Logger.Write(this, LogLevel.Debug, "Closing window: {0}", id);
                window.Close();
                return true;
            }

            public void AddCallback(IEnumerable<string> ids, EventHandler handler, bool once = false)
            {
                foreach (var id in ids)
                {
                    this.AddCallback(id, handler, once);
                }
            }

            public void AddCallback(string id, EventHandler handler, bool once = false)
            {
                var registration = default(WindowRegistration);
                if (this.TryGet(id, out registration))
                {
                    handler(registration, EventArgs.Empty);
                    return;
                }
                if (once)
                {
                    handler = CreateOneTimeCallback(id, handler);
                }
                var callbacks = this.Callbacks.GetOrAdd(id, () => new HashSet<EventHandler>());
                callbacks.Add(handler);
            }

            public void RemoveCallback(IEnumerable<string> ids, EventHandler handler)
            {
                foreach (var id in ids)
                {
                    this.RemoveCallback(id, handler);
                }
            }

            public void RemoveCallback(string id, EventHandler handler)
            {
                var callbacks = default(ISet<EventHandler>);
                if (!this.Callbacks.TryGetValue(id, out callbacks))
                {
                    return;
                }
                callbacks.Remove(handler);
            }

            public void AddCreated(IEnumerable<string> ids, EventHandler handler)
            {
                foreach (var id in ids)
                {
                    this.AddCreated(id, handler);
                }
            }

            public void AddCreated(string id, EventHandler handler)
            {
                this.AddCallback(id, (sender, e) =>
                {
                    if (sender is WindowRegistration registration)
                    {
                        registration.Created += handler;
                    }
                }, true);
            }

            public void RemoveCreated(IEnumerable<string> ids, EventHandler handler)
            {
                foreach (var id in ids)
                {
                    this.RemoveCreated(id, handler);
                }
            }

            public void RemoveCreated(string id, EventHandler handler)
            {
                var registration = default(WindowRegistration);
                if (this.TryGet(id, out registration))
                {
                    registration.Created -= handler;
                }
            }

            public void AddIsVisibleChanged(IEnumerable<string> ids, EventHandler handler)
            {
                foreach (var id in ids)
                {
                    this.AddIsVisibleChanged(id, handler);
                }
            }

            public void AddIsVisibleChanged(string id, EventHandler handler)
            {
                this.AddCallback(id, (sender, e) =>
                {
                    if (sender is WindowRegistration registration)
                    {
                        registration.IsVisibleChanged += handler;
                    }
                }, true);
            }

            public void RemoveIsVisibleChanged(IEnumerable<string> ids, EventHandler handler)
            {
                foreach (var id in ids)
                {
                    this.RemoveIsVisibleChanged(id, handler);
                }
            }

            public void RemoveIsVisibleChanged(string id, EventHandler handler)
            {
                var registration = default(WindowRegistration);
                if (this.TryGet(id, out registration))
                {
                    registration.IsVisibleChanged -= handler;
                }
            }

            public void AddClosed(IEnumerable<string> ids, EventHandler handler)
            {
                foreach (var id in ids)
                {
                    this.AddClosed(id, handler);
                }
            }

            public void AddClosed(string id, EventHandler handler)
            {
                this.AddCallback(id, (sender, e) =>
                {
                    if (sender is WindowRegistration registration)
                    {
                        registration.Closed += handler;
                    }
                }, true);
            }

            public void RemoveClosed(IEnumerable<string> ids, EventHandler handler)
            {
                foreach (var id in ids)
                {
                    this.RemoveClosed(id, handler);
                }
            }

            public void RemoveClosed(string id, EventHandler handler)
            {
                var registration = default(WindowRegistration);
                if (this.TryGet(id, out registration))
                {
                    registration.Closed -= handler;
                }
            }

            private static EventHandler CreateOneTimeCallback(string id, EventHandler handler)
            {
                return (sender, e) =>
                {
                    handler(sender, e);
                    Registrations.RemoveCallback(id, handler);
                };
            }
        }

        public class WindowRegistration : BaseComponent
        {
            public WindowRegistration(string id, UserInterfaceWindowRole role, Func<Window> factory)
            {
                this.Id = id;
                this.Role = role;
                this.Factory = factory;
                this.Instance = new ResettableLazy<Window>(this.Create(Factory));
            }

            public string Id { get; private set; }

            public UserInterfaceWindowRole Role { get; private set; }

            public Func<Window> Factory { get; private set; }

            public ResettableLazy<Window> Instance { get; private set; }

            protected virtual Func<Window> Create(Func<Window> factory)
            {
                return () =>
                {
                    Logger.Write(this, LogLevel.Debug, "Creating window: {0}", this.Id);
                    var window = factory();
                    Logger.Write(this, LogLevel.Debug, "Created window: {0}/{1}", window.GetType().Name, window.Title);
                    window.Owner = Windows.ActiveWindow;
                    window.IsVisibleChanged += this.OnIsVisibleChanged;
                    window.Closed += this.OnClosed;
                    this.OnCreated(window, EventArgs.Empty);
                    return window;
                };
            }

            protected virtual void OnCreated(object sender, EventArgs e)
            {
                if (this.Created != null)
                {
                    this.Created(sender, e);
                }
            }

            public event EventHandler Created;

            protected virtual void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
            {
                if (this.Role == UserInterfaceWindowRole.Main)
                {
                    Logger.Write(this, LogLevel.Debug, "Main window visiblity changed, refreshing active window: {0}", this.Id);
                    OnActiveWindowChanged();
                }
                if (this.IsVisibleChanged != null)
                {
                    this.IsVisibleChanged(sender, EventArgs.Empty);
                }
            }

            public event EventHandler IsVisibleChanged;

            protected virtual void OnClosed(object sender, EventArgs e)
            {
                Logger.Write(this, LogLevel.Debug, "Window was closed: {0}", this.Id);
                this.Reset();
                if (this.Closed != null)
                {
                    this.Closed(sender, e);
                }
                if (this.Role == UserInterfaceWindowRole.Main)
                {
                    Logger.Write(this, LogLevel.Debug, "Main window was closed, attempting shutdown: {0}", this.Id);
                    var task = TryShutdown();
                }
            }

            public event EventHandler Closed;

            public bool IsCreated
            {
                get
                {
                    return this.Instance.IsValueCreated;
                }
            }

            public Window GetInstance()
            {
                return this.Instance.Value;
            }

            public bool TryGetInstance(out Window instance)
            {
                if (!this.IsCreated)
                {
                    instance = default(Window);
                    return false;
                }
                instance = this.Instance.Value;
                return true;
            }

            public void Reset()
            {
                var window = default(Window);
                if (this.TryGetInstance(out window))
                {
                    window.IsVisibleChanged -= this.OnIsVisibleChanged;
                    window.Closed -= this.OnClosed;
                }
                this.Instance.Reset();
            }
        }

        public static async Task<bool> ShowDialog<T>(ICore core, string title) where T : new()
        {
            var result = default(bool);
            await Invoke(() =>
            {
                var content = new T();
                var window = new DialogWindow<T>(content)
                {
                    Topmost = true,
                    Title = title
                };
                result = window.ShowDialog().GetValueOrDefault();
            }).ConfigureAwait(false);
            return result;
        }

        public static async Task<bool> ShowDialog<T>(ICore core, string title, T content)
        {
            var result = default(bool);
            await Invoke(() =>
            {
                var window = new DialogWindow<T>(content)
                {
                    Topmost = true,
                    Title = title
                };
                result = window.ShowDialog().GetValueOrDefault();
            }).ConfigureAwait(false);
            return result;
        }

        private class DialogWindow<T> : WindowBase
        {
            private DialogWindow()
            {
                if (!global::FoxTunes.Properties.Settings.Default.SettingsWindowBounds.IsEmpty())
                {
                    if (ScreenHelper.WindowBoundsVisible(global::FoxTunes.Properties.Settings.Default.SettingsWindowBounds))
                    {
                        this.Left = global::FoxTunes.Properties.Settings.Default.SettingsWindowBounds.Left;
                        this.Top = global::FoxTunes.Properties.Settings.Default.SettingsWindowBounds.Top;
                    }
                    this.Width = global::FoxTunes.Properties.Settings.Default.SettingsWindowBounds.Width;
                    this.Height = global::FoxTunes.Properties.Settings.Default.SettingsWindowBounds.Height;
                }
                else
                {
                    this.Width = 800;
                    this.Height = 600;
                }
                if (double.IsNaN(this.Left) || double.IsNaN(this.Top))
                {
                    this.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                }
                this.InitializeComponent();
            }

            public DialogWindow(T content) : this()
            {
                this.Content = content;
            }

            public override string Id
            {
                get
                {
                    return "3AA74E66-005F-4772-99BE-31C18C317D23";
                }
            }

            protected virtual void InitializeComponent()
            {
                ButtonExtensions.AddCommandExecutedHandler(this, this.OnCommandExecuted);
            }

            protected virtual void OnCommandExecuted(object sender, ButtonExtensions.CommandExecutedEventArgs e)
            {
                if (string.Equals(e.Behaviour, ButtonExtensions.COMMAND_BEHAVIOUR_DISMISS, StringComparison.OrdinalIgnoreCase))
                {
                    this.Close();
                }
                else if (string.Equals(e.Behaviour, ButtonExtensions.COMMAND_BEHAVIOUR_ACCEPT, StringComparison.OrdinalIgnoreCase))
                {
                    this.DialogResult = true;
                }
            }

            protected override void OnClosing(CancelEventArgs e)
            {
                global::FoxTunes.Properties.Settings.Default.SettingsWindowBounds = this.RestoreBounds;
                global::FoxTunes.Properties.Settings.Default.Save();
                base.OnClosing(e);
            }
        }
    }
}
