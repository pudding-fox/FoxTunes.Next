using FoxTunes.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Interop;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    [PlatformDependency(Major = 6, Minor = 1)]
    public class TaskbarButtonsBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        const int UPDATE_INTERVAL = 1000;

        private static readonly object SyncRoot = new object();

        const int BUTTON_PREVIOUS = 0;

        const int BUTTON_PLAY_PAUSE = 1;

        const int BUTTON_NEXT = 2;

        const int WM_SHOWWINDOW = 0x0018;

        public TaskbarButtonsBehaviour()
        {
            this.Callback = new HwndSourceHook(this.OnCallback);
            this.Windows = new ConcurrentDictionary<IntPtr, TaskbarButtonsWindowFlags>();
            this.ImageLists = new ConcurrentDictionary<IntPtr, IntPtr>();
        }

        public Timer Timer { get; private set; }

        public HwndSourceHook Callback { get; private set; }

        public ConcurrentDictionary<IntPtr, TaskbarButtonsWindowFlags> Windows { get; private set; }

        protected virtual bool HasFlags(IntPtr handle)
        {
            var flags = default(TaskbarButtonsWindowFlags);
            return this.Windows.TryGetValue(handle, out flags);
        }

        protected virtual bool HasFlag(IntPtr handle, TaskbarButtonsWindowFlags flag)
        {
            var flags = default(TaskbarButtonsWindowFlags);
            if (!this.Windows.TryGetValue(handle, out flags))
            {
                return false;
            }
            return flags.HasFlag(flag);
        }

        protected virtual bool AddFlag(IntPtr handle, TaskbarButtonsWindowFlags flag)
        {
            var flags = default(TaskbarButtonsWindowFlags);
            if (!this.Windows.TryGetValue(handle, out flags))
            {
                return false;
            }
            return this.Windows.TryUpdate(handle, flags | flag, flags);
        }

        protected virtual bool RemoveFlag(IntPtr handle, TaskbarButtonsWindowFlags flag)
        {
            var flags = default(TaskbarButtonsWindowFlags);
            if (!this.Windows.TryGetValue(handle, out flags))
            {
                return false;
            }
            return this.Windows.TryUpdate(handle, flags & ~flag, flags);
        }

        public ConcurrentDictionary<IntPtr, IntPtr> ImageLists { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public BooleanConfigurationElement Enabled { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.PlaylistManager = core.Managers.Playlist;
            this.PlaybackManager = core.Managers.Playback;
            this.UserInterface = core.Components.UserInterface;
            this.UserInterface.WindowCreated += this.OnWindowCreated;
            this.UserInterface.WindowDestroyed += this.OnWindowDestroyed;
            this.UserInterface.ShuttingDown += this.OnShuttingDown;
            this.Configuration = core.Components.Configuration;
            this.Enabled = this.Configuration.GetElement<BooleanConfigurationElement>(
                TaskbarButtonsBehaviourConfiguration.SECTION,
                TaskbarButtonsBehaviourConfiguration.ENABLED_ELEMENT
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
                //Only create taskbar buttons for main windows.
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Window created: {0}", e.Window.Handle);
            this.Windows.TryAdd(e.Window.Handle, TaskbarButtonsWindowFlags.None);
        }

        protected virtual void OnWindowDestroyed(object sender, UserInterfaceWindowEventArgs e)
        {
            if (e.Window.Role != UserInterfaceWindowRole.Main)
            {
                //Only create taskbar buttons for main windows.
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Window destroyed: {0}", e.Window.Handle);
            this.AddFlag(e.Window.Handle, TaskbarButtonsWindowFlags.Destroyed);
        }

        protected virtual void OnWindowIsVisibleChanged(IntPtr handle, bool visible)
        {
            if (!visible)
            {
                Logger.Write(this, LogLevel.Debug, "Window was hidden: {0}", handle);
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Window was shown: {0}", handle); 
                if (this.HasFlag(handle, TaskbarButtonsWindowFlags.ImagesCreated))
                {
                    this.DestroyImages(handle);
                }
                this.RemoveFlag(handle, TaskbarButtonsWindowFlags.ButtonsCreated);
            }
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

        protected virtual async Task Update(IntPtr handle, TaskbarButtonsWindowFlags flags)
        {
            if (flags.HasFlag(TaskbarButtonsWindowFlags.Destroyed))
            {
                if (flags.HasFlag(TaskbarButtonsWindowFlags.Registered))
                {
                    this.RemoveHook(handle);
                }
                if (flags.HasFlag(TaskbarButtonsWindowFlags.ImagesCreated))
                {
                    this.DestroyImages(handle);
                }
                //TODO: We should remove the window from our table but it causes issues and we're likely shutting down now.
                return;
            }
            if (flags.HasFlag(TaskbarButtonsWindowFlags.Error))
            {
                return;
            }
            if (this.Enabled.Value)
            {
                if (!flags.HasFlag(TaskbarButtonsWindowFlags.Registered))
                {
                    this.AddHook(handle);
                }
                if (!flags.HasFlag(TaskbarButtonsWindowFlags.ImagesCreated))
                {
                    if (!await this.CreateImages(handle).ConfigureAwait(false))
                    {
                        return;
                    }
                }
                if (!flags.HasFlag(TaskbarButtonsWindowFlags.ButtonsCreated))
                {
                    if (!await this.CreateButtons(handle).ConfigureAwait(false))
                    {
                        return;
                    }
                }
                else
                {
                    if (!await this.UpdateButtons(handle).ConfigureAwait(false))
                    {
                        return;
                    }
                }
            }
            else
            {
                //Although we're no longer enabled, these features can't be disabled so we must keep them updated.
                if (flags.HasFlag(TaskbarButtonsWindowFlags.ButtonsCreated))
                {
                    if (!await this.UpdateButtons(handle).ConfigureAwait(false))
                    {
                        return;
                    }
                }
            }
        }

        protected virtual IntPtr OnCallback(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WindowsTaskbarList.WM_COMMAND)
            {
                var low = (wParam.ToInt64() & 0x0000FFFF);
                var high = (wParam.ToInt64() & 0xFFFF0000) >> 16;
                if (high == WindowsTaskbarList.THBN_CLICKED)
                {
                    this.OnButtonPressed(Convert.ToInt32(low));
                }
            }
            else if (msg == WM_SHOWWINDOW)
            {
                var visiblity = wParam.ToInt32();
                this.OnWindowIsVisibleChanged(hwnd, visiblity == 1);
            }
            else if (msg == WindowMessages.WM_TASKBARCREATED)
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
            this.Windows[handle] = TaskbarButtonsWindowFlags.Registered;
        }

        protected virtual void AddHook(IntPtr handle)
        {
            Logger.Write(this, LogLevel.Debug, "Adding Windows event handler.");
            var source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                Logger.Write(this, LogLevel.Warn, "No such window for handle: {0}", handle);
                this.AddFlag(handle, TaskbarButtonsWindowFlags.Error);
                return;
            }
            source.AddHook(this.Callback);
            this.AddFlag(handle, TaskbarButtonsWindowFlags.Registered);
        }

        protected virtual void RemoveHook(IntPtr handle)
        {
            Logger.Write(this, LogLevel.Debug, "Removing Windows event handler.");
            var source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                Logger.Write(this, LogLevel.Warn, "No such window for handle: {0}", handle);
                this.AddFlag(handle, TaskbarButtonsWindowFlags.Error);
                return;
            }
            source.RemoveHook(this.Callback);
            this.RemoveFlag(handle, TaskbarButtonsWindowFlags.Registered);
        }

        protected virtual async Task<bool> CreateImages(IntPtr handle)
        {
            Logger.Write(this, LogLevel.Debug, "Creating taskbar button image list.");
            var source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                Logger.Write(this, LogLevel.Warn, "No such window for handle: {0}", handle);
                this.AddFlag(handle, TaskbarButtonsWindowFlags.Error);
                return false;
            }
            var width = WindowsSystemMetrics.GetSystemMetrics(
                WindowsSystemMetrics.SystemMetric.SM_CXSMICON
            );
            var height = WindowsSystemMetrics.GetSystemMetrics(
                WindowsSystemMetrics.SystemMetric.SM_CYSMICON
            );
            Logger.Write(this, LogLevel.Debug, "Taskbar buttom image dimentions: {0}x{1}", width, height);
            var imageList = WindowsImageList.ImageList_Create(
                width,
                height,
                WindowsImaging.ILC_COLOR32,
                4,
                0
            );
            if (IntPtr.Zero.Equals(imageList))
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create button image list.");
                this.AddFlag(handle, TaskbarButtonsWindowFlags.Error);
                return false;
            }
            else
            {
                //TODO: Potentially overwriting the previous value.
                this.ImageLists[handle] = imageList;
            }
            for (var a = 0; a < 4; a++)
            {
                using (var bitmap = this.GetImage(a, width, height))
                {
                    if (!this.AddImage(handle, imageList, bitmap, width, height))
                    {
                        return false;
                    }
                }
            }
            var result = default(WindowsTaskbarList.HResult);
            await source.Invoke(
                () => result = WindowsTaskbarList.Instance.ThumbBarSetImageList(handle, imageList)
            ).ConfigureAwait(false);
            if (result != WindowsTaskbarList.HResult.Ok)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create button image list: {0}", Enum.GetName(typeof(WindowsTaskbarList.HResult), result));
                this.AddFlag(handle, TaskbarButtonsWindowFlags.Error);
                return false;
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Taskbar button image list created.");
                this.AddFlag(handle, TaskbarButtonsWindowFlags.ImagesCreated);
                return true;
            }
        }

        protected virtual bool AddImage(IntPtr handle, IntPtr imageList, Bitmap bitmap, int width, int height)
        {
            using (var hdc = WindowsImaging.ScopedDC.Compatible(handle))
            {
                if (!hdc.IsValid)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to create device context.");
                    this.AddFlag(handle, TaskbarButtonsWindowFlags.Error);
                    return false;
                }
                var bitmapSection = default(IntPtr);
                if (!WindowsImaging.CreateDIBSection(hdc.cdc, bitmap, bitmap.Width, bitmap.Height, out bitmapSection))
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to create native bitmap.");
                    this.AddFlag(handle, TaskbarButtonsWindowFlags.Error);
                    return false;
                }
                var result = WindowsImageList.ImageList_Add(
                    imageList,
                    bitmapSection,
                    IntPtr.Zero
                );
                WindowsImaging.DeleteObject(bitmapSection);
                if (result < 0)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to add image to ImageList.");
                    this.AddFlag(handle, TaskbarButtonsWindowFlags.Error);
                    return false;
                }
            }
            return true;
        }

        protected virtual bool DestroyImages(IntPtr handle)
        {
            var imageList = default(IntPtr);
            if (!this.ImageLists.TryGetValue(handle, out imageList))
            {
                //There was no image list to destroy.
                return true;
            }
            var result = WindowsImageList.ImageList_Destroy(imageList);
            if (!result)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to destroy image list: {0}", Enum.GetName(typeof(WindowsTaskbarList.HResult), result));
                this.AddFlag(handle, TaskbarButtonsWindowFlags.Error);
                return false;
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Taskbar button image list destroyed.");
                this.RemoveFlag(handle, TaskbarButtonsWindowFlags.ImagesCreated);
                return true;
            }
        }

        protected virtual Bitmap GetImage(int index, int width, int height)
        {
            var name = string.Format("FoxTunes.Core.Windows.Images.{0}.png", this.GetImageName(index));
            Logger.Write(this, LogLevel.Debug, "Creating image: {0}", name);
            using (var stream = typeof(TaskbarButtonsBehaviour).Assembly.GetManifestResourceStream(name))
            {
                return this.GetImage(stream, width, height);
            }
        }

        protected virtual Bitmap GetImage(Stream stream, int width, int height)
        {
            var image = (Bitmap)Bitmap.FromStream(stream);
            if (image.Width != width || image.Height != height)
            {
                try
                {
                    return WindowsImaging.Resize(image, width, height, false);
                }
                finally
                {
                    image.Dispose();
                }
            }
            return image;
        }

        protected virtual string GetImageName(int index)
        {
            switch (index)
            {
                case 0:
                    return "prev";
                case 1:
                    return "play";
                case 2:
                    return "pause";
                case 3:
                    return "next";
            }
            throw new NotImplementedException();
        }

        protected virtual async Task<bool> CreateButtons(IntPtr handle)
        {
            Logger.Write(this, LogLevel.Debug, "Creating taskbar buttons.");
            var source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                Logger.Write(this, LogLevel.Warn, "No such window for handle: {0}", handle);
                return false;
            }
            var buttons = this.Buttons.ToArray();
            var result = default(WindowsTaskbarList.HResult);
            await source.Invoke(
                () => result = WindowsTaskbarList.Instance.ThumbBarAddButtons(
                    handle,
                    Convert.ToUInt32(buttons.Length),
                    buttons
                )
            ).ConfigureAwait(false);
            if (result != WindowsTaskbarList.HResult.Ok)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create taskbar buttons: {0}", Enum.GetName(typeof(WindowsTaskbarList.HResult), result));
                this.AddFlag(handle, TaskbarButtonsWindowFlags.Error);
                return false;
            }
            else
            {
                Logger.Write(this, LogLevel.Debug, "Taskbar buttons created.");
                this.AddFlag(handle, TaskbarButtonsWindowFlags.ButtonsCreated);
                return true;
            }
        }

        protected virtual async Task UpdateButtons()
        {
            foreach (var pair in this.Windows)
            {
                await this.UpdateButtons(pair.Key).ConfigureAwait(false);
            }
        }

        protected virtual async Task<bool> UpdateButtons(IntPtr handle)
        {
            var source = HwndSource.FromHwnd(handle);
            if (source == null)
            {
                Logger.Write(this, LogLevel.Warn, "No such window for handle: {0}", handle);
                return false;
            }
            var buttons = this.Buttons.ToArray();
            var result = default(WindowsTaskbarList.HResult);
            await source.Invoke(
                () => result = WindowsTaskbarList.Instance.ThumbBarUpdateButtons(
                    handle,
                    Convert.ToUInt32(buttons.Length),
                    buttons
                )
            ).ConfigureAwait(false);
            if (result != WindowsTaskbarList.HResult.Ok)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to update taskbar buttons: {0}", Enum.GetName(typeof(WindowsTaskbarList.HResult), result));
                this.AddFlag(handle, TaskbarButtonsWindowFlags.Error);
                return false;
            }
            else
            {
                return true;
            }
        }

        protected virtual IEnumerable<WindowsTaskbarList.ThumbButton> Buttons
        {
            get
            {
                yield return this.PreviousButton;
                yield return this.PlayPauseButton;
                yield return this.NextButton;
            }
        }

        protected virtual WindowsTaskbarList.ThumbButton PreviousButton
        {
            get
            {
                return new WindowsTaskbarList.ThumbButton()
                {
                    Id = BUTTON_PREVIOUS,
                    Tip = Strings.TaskbarButtonsBehaviour_Previous,
                    Bitmap = 0,
                    Mask = this.ButtonMask,
                    Flags = this.ButtonFlags
                };
            }
        }

        protected virtual WindowsTaskbarList.ThumbButton PlayPauseButton
        {
            get
            {
                var outputStream = this.PlaybackManager.CurrentStream;
                return new WindowsTaskbarList.ThumbButton()
                {
                    Id = BUTTON_PLAY_PAUSE,
                    Tip = Strings.TaskbarButtonsBehaviour_PlayPause,
                    Bitmap = outputStream != null && outputStream.IsPlaying ? 2u : 1u,
                    Mask = this.ButtonMask,
                    Flags = this.ButtonFlags
                };
            }
        }

        protected virtual WindowsTaskbarList.ThumbButton NextButton
        {
            get
            {
                return new WindowsTaskbarList.ThumbButton()
                {
                    Id = BUTTON_NEXT,
                    Tip = Strings.TaskbarButtonsBehaviour_Next,
                    Bitmap = 3,
                    Mask = this.ButtonMask,
                    Flags = this.ButtonFlags
                };
            }
        }

        protected virtual WindowsTaskbarList.ThumbButtonMask ButtonMask
        {
            get
            {
                return WindowsTaskbarList.ThumbButtonMask.THB_FLAGS | WindowsTaskbarList.ThumbButtonMask.Bitmap;
            }
        }

        protected virtual WindowsTaskbarList.ThumbButtonOptions ButtonFlags
        {
            get
            {
                var flags = default(WindowsTaskbarList.ThumbButtonOptions);
                if (!this.Enabled.Value)
                {
                    flags |= WindowsTaskbarList.ThumbButtonOptions.Hidden;
                }
                return flags;
            }
        }

        protected virtual void OnButtonPressed(int button)
        {
            var task = default(Task);
            switch (button)
            {
                case BUTTON_PREVIOUS:
                    task = this.Previous();
                    break;
                case BUTTON_PLAY_PAUSE:
                    task = this.PlayPause();
                    break;
                case BUTTON_NEXT:
                    task = this.Next();
                    break;
            }
        }

        protected virtual async Task Previous()
        {
            Logger.Write(this, LogLevel.Debug, "Previous button was clicked.");
            await this.PlaylistManager.Previous().ConfigureAwait(false);
            var task = this.UpdateButtons();
        }

        protected virtual async Task PlayPause()
        {
            Logger.Write(this, LogLevel.Debug, "Play/pause button was clicked.");
            var currentStream = this.PlaybackManager.CurrentStream;
            if (currentStream == null)
            {
                await this.PlaylistManager.Next().ConfigureAwait(false);
            }
            else
            {
                if (currentStream.IsPaused)
                {
                    await currentStream.Resume().ConfigureAwait(false);
                }
                else if (currentStream.IsPlaying)
                {
                    await currentStream.Pause().ConfigureAwait(false);
                }
            }
            var task = this.UpdateButtons();
        }

        protected virtual async Task Next()
        {
            Logger.Write(this, LogLevel.Debug, "Next button was clicked.");
            await this.PlaylistManager.Next().ConfigureAwait(false);
            var task = this.UpdateButtons();
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return TaskbarButtonsBehaviourConfiguration.GetConfigurationSections();
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

        ~TaskbarButtonsBehaviour()
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
        public static extern int RegisterWindowMessage(string msg);
    }

    [Flags]
    public enum TaskbarButtonsWindowFlags : byte
    {
        None = 0,
        Registered = 1,
        ImagesCreated = 2,
        ButtonsCreated = 4,
        Error = 8,
        Destroyed = 16
    }
}
