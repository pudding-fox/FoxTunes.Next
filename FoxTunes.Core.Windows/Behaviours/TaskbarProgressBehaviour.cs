using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Interop;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    [PlatformDependency(Major = 6, Minor = 1)]
    public class TaskbarProgressBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        const int UPDATE_INTERVAL = 1000;

        private static readonly object SyncRoot = new object();

        public TaskbarProgressBehaviour()
        {
            this.Callback = new HwndSourceHook(this.OnCallback);
            this.Windows = new ConcurrentDictionary<IntPtr, TaskbarProgressWindowFlags>();
        }

        public Timer Timer { get; private set; }

        public HwndSourceHook Callback { get; private set; }

        public ConcurrentDictionary<IntPtr, TaskbarProgressWindowFlags> Windows { get; private set; }

        protected virtual bool HasFlags(IntPtr handle)
        {
            var flags = default(TaskbarProgressWindowFlags);
            return this.Windows.TryGetValue(handle, out flags);
        }

        protected virtual bool HasFlag(IntPtr handle, TaskbarProgressWindowFlags flag)
        {
            var flags = default(TaskbarProgressWindowFlags);
            if (!this.Windows.TryGetValue(handle, out flags))
            {
                return false;
            }
            return flags.HasFlag(flag);
        }

        protected virtual bool AddFlag(IntPtr handle, TaskbarProgressWindowFlags flag)
        {
            var flags = default(TaskbarProgressWindowFlags);
            if (!this.Windows.TryGetValue(handle, out flags))
            {
                return false;
            }
            return this.Windows.TryUpdate(handle, flags | flag, flags);
        }

        protected virtual bool RemoveFlag(IntPtr handle, TaskbarProgressWindowFlags flag)
        {
            var flags = default(TaskbarProgressWindowFlags);
            if (!this.Windows.TryGetValue(handle, out flags))
            {
                return false;
            }
            return this.Windows.TryUpdate(handle, flags & ~flag, flags);
        }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.UserInterface = core.Components.UserInterface;
            this.UserInterface.WindowCreated += this.OnWindowCreated;
            this.UserInterface.WindowDestroyed += this.OnWindowDestroyed;
            this.UserInterface.ShuttingDown += this.OnShuttingDown;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                TaskbarProgressBehaviourConfiguration.SECTION,
                TaskbarProgressBehaviourConfiguration.ENABLED_ELEMENT
            );
            this.Enabled.ConnectValue(value =>
            {
                if (value)
                {
                    this.Enable();
                }
                else
                {
                    this.Disable();
                }
            });
            base.InitializeComponent(core);
        }

        protected virtual void OnWindowCreated(object sender, UserInterfaceWindowEventArgs e)
        {
            if (e.Window.Role != UserInterfaceWindowRole.Main)
            {
                //Only create taskbar progress for main windows.
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Window created: {0}", e.Window.Handle);
            this.Windows.TryAdd(e.Window.Handle, TaskbarProgressWindowFlags.None);
        }

        protected virtual void OnWindowDestroyed(object sender, UserInterfaceWindowEventArgs e)
        {
            if (e.Window.Role != UserInterfaceWindowRole.Main)
            {
                //Only create taskbar progress for main windows.
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Window destroyed: {0}", e.Window.Handle);
            this.AddFlag(e.Window.Handle, TaskbarProgressWindowFlags.Destroyed);
        }

        protected virtual void OnShuttingDown(object sender, EventArgs e)
        {
            Logger.Write(this, LogLevel.Debug, "Shutdown signal recieved.");
            this.Windows.Clear();
        }

        public void Enable()
        {
            lock (SyncRoot)
            {
                if (this.Timer == null)
                {
                    this.Timer = new Timer();
                    this.Timer.Interval = UPDATE_INTERVAL;
                    this.Timer.Elapsed += this.OnElapsed;
                    this.Timer.AutoReset = false;
                    this.Timer.Start();
                    Logger.Write(this, LogLevel.Debug, "Updater enabled.");
                }
            }
        }

        public void Disable()
        {
            lock (SyncRoot)
            {
                if (this.Timer != null)
                {
                    this.Timer.Stop();
                    this.Timer.Elapsed -= this.OnElapsed;
                    this.Timer.Dispose();
                    this.Timer = null;
                    Logger.Write(this, LogLevel.Debug, "Updater disabled.");
                }
            }
            //Perform any cleanup.
            var task = this.Update();
        }

        protected virtual async void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                await this.Update().ConfigureAwait(false);
                lock (SyncRoot)
                {
                    if (this.Timer != null)
                    {
                        this.Timer.Start();
                    }
                }
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }

        protected virtual async Task Update()
        {
            foreach (var pair in this.Windows)
            {
                await this.Update(pair.Key, pair.Value).ConfigureAwait(false);
            }
        }

        protected virtual async Task Update(IntPtr handle, TaskbarProgressWindowFlags flags)
        {
            if (flags.HasFlag(TaskbarProgressWindowFlags.Destroyed))
            {
                if (flags.HasFlag(TaskbarProgressWindowFlags.Registered))
                {
                    this.RemoveHook(handle);
                }
                if (flags.HasFlag(TaskbarProgressWindowFlags.ProgressCreated))
                {
                    if (!await this.ClearProgress(handle).ConfigureAwait(false))
                    {
                        return;
                    }
                }
                //TODO: We should remove the window from our table but it causes issues and we're likely shutting down now.
                return;
            }
            if (flags.HasFlag(TaskbarProgressWindowFlags.Error))
            {
                return;
            }
            if (this.Enabled.Value)
            {
                if (!flags.HasFlag(TaskbarProgressWindowFlags.Registered))
                {
                    this.AddHook(handle);
                }
                if (!await this.UpdateProgress(handle).ConfigureAwait(false))
                {
                    return;
                }
            }
            else
            {
                if (flags.HasFlag(TaskbarProgressWindowFlags.ProgressCreated))
                {
                    if (!await this.ClearProgress(handle).ConfigureAwait(false))
                    {
                        return;
                    }
                }
            }
        }

        protected virtual IntPtr OnCallback(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WindowMessages.WM_TASKBARCREATED)
            {
                this.OnTaskBarCreated(hwnd);
            }
            return IntPtr.Zero;
        }

        protected virtual void OnTaskBarCreated(IntPtr handle)
        {
            if (this.HasFlags(handle))
            {
                //Handing an event for an unknown window?
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Taskbar was created: {0}", handle);
            this.Windows[handle] = TaskbarProgressWindowFlags.Registered;
        }

        protected virtual void AddHook(IntPtr handle)
        {
            Logger.Write(this, LogLevel.Debug, "Adding Windows event handler.");
            var source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                Logger.Write(this, LogLevel.Warn, "No such window for handle: {0}", handle);
                this.AddFlag(handle, TaskbarProgressWindowFlags.Error);
                return;
            }
            source.AddHook(this.Callback);
            this.AddFlag(handle, TaskbarProgressWindowFlags.Registered);
        }

        protected virtual void RemoveHook(IntPtr handle)
        {
            Logger.Write(this, LogLevel.Debug, "Removing Windows event handler.");
            var source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                Logger.Write(this, LogLevel.Warn, "No such window for handle: {0}", handle);
                this.AddFlag(handle, TaskbarProgressWindowFlags.Error);
                return;
            }
            source.RemoveHook(this.Callback);
            this.RemoveFlag(handle, TaskbarProgressWindowFlags.Registered);
        }

        protected virtual async Task<bool> UpdateProgress(IntPtr handle)
        {
            if (await this.UpdateTaskProgress(handle).ConfigureAwait(false))
            {
                return true;
            }
            if (await this.UpdatePlaybackProgress(handle).ConfigureAwait(false))
            {
                return true;
            }
            if (this.HasFlag(handle, TaskbarProgressWindowFlags.ProgressCreated))
            {
                return await this.ClearProgress(handle).ConfigureAwait(false);
            }
            return true;
        }

        protected virtual async Task<bool> UpdateTaskProgress(IntPtr handle)
        {
            var backgroundTask = BackgroundTask.Active.FirstOrDefault(_backgroundTask => _backgroundTask.Visible);
            if (backgroundTask == null)
            {
                return false;
            }
            var source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                Logger.Write(this, LogLevel.Warn, "No such window for handle: {0}", handle);
                return false;
            }
            var result = default(WindowsTaskbarList.HResult);
            await source.Invoke(
                () =>
                {
                    if (backgroundTask.Count != 0)
                    {
                        result = WindowsTaskbarList.Instance.SetProgressValue(
                            handle,
                            Convert.ToUInt64(backgroundTask.Position),
                            Convert.ToUInt64(backgroundTask.Count)
                        );
                    }
                    else
                    {
                        result = WindowsTaskbarList.Instance.SetProgressState(
                            handle,
                            WindowsTaskbarList.TaskbarProgressBarStatus.Indeterminate
                        );
                    }
                }
            ).ConfigureAwait(false);
            if (result != WindowsTaskbarList.HResult.Ok)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to update taskbar progress: {0}", Enum.GetName(typeof(WindowsTaskbarList.HResult), result));
                this.AddFlag(handle, TaskbarProgressWindowFlags.Error);
                return false;
            }
            else
            {
                this.AddFlag(handle, TaskbarProgressWindowFlags.ProgressCreated);
                return true;
            }
        }

        protected virtual async Task<bool> UpdatePlaybackProgress(IntPtr handle)
        {
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream == null)
            {
                return false;
            }
            var source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                Logger.Write(this, LogLevel.Warn, "No such window for handle: {0}", handle);
                return false;
            }
            var result = default(WindowsTaskbarList.HResult);
            await source.Invoke(
                () =>
                {
                    result = WindowsTaskbarList.Instance.SetProgressValue(
                        handle,
                        Convert.ToUInt64(outputStream.Position),
                        Convert.ToUInt64(outputStream.Length)
                    );
                    if (outputStream.IsPaused && result == WindowsTaskbarList.HResult.Ok)
                    {
                        result = WindowsTaskbarList.Instance.SetProgressState(
                            handle,
                            WindowsTaskbarList.TaskbarProgressBarStatus.Paused
                        );
                    }
                    else
                    {
                        result = WindowsTaskbarList.Instance.SetProgressState(
                            handle,
                            WindowsTaskbarList.TaskbarProgressBarStatus.Normal
                        );
                    }
                }
            ).ConfigureAwait(false);
            if (result != WindowsTaskbarList.HResult.Ok)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to update taskbar progress: {0}", Enum.GetName(typeof(WindowsTaskbarList.HResult), result));
                this.AddFlag(handle, TaskbarProgressWindowFlags.Error);
                return false;
            }
            else
            {
                this.AddFlag(handle, TaskbarProgressWindowFlags.ProgressCreated);
                return true;
            }
        }

        protected virtual async Task<bool> ClearProgress(IntPtr handle)
        {
            var source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                Logger.Write(this, LogLevel.Warn, "No such window for handle: {0}", handle);
                return false;
            }
            var result = default(WindowsTaskbarList.HResult);
            await source.Invoke(
                () => result = WindowsTaskbarList.Instance.SetProgressState(
                    handle,
                    WindowsTaskbarList.TaskbarProgressBarStatus.NoProgress
                )
            ).ConfigureAwait(false);
            if (result != WindowsTaskbarList.HResult.Ok)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to update taskbar progress: {0}", Enum.GetName(typeof(WindowsTaskbarList.HResult), result));
                this.AddFlag(handle, TaskbarProgressWindowFlags.Error);
                return false;
            }
            this.RemoveFlag(handle, TaskbarProgressWindowFlags.ProgressCreated);
            return true;
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return TaskbarProgressBehaviourConfiguration.GetConfigurationSections();
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
            if (this.UserInterface != null)
            {
                this.UserInterface.WindowCreated -= this.OnWindowCreated;
                this.UserInterface.WindowDestroyed -= this.OnWindowDestroyed;
                this.UserInterface.ShuttingDown -= this.OnShuttingDown;
            }
            this.Disable();
        }

        ~TaskbarProgressBehaviour()
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
    }

    [Flags]
    public enum TaskbarProgressWindowFlags : byte
    {
        None = 0,
        Registered = 1,
        Error = 8,
        Destroyed = 16,
        ProgressCreated = 32
    }
}
