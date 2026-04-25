using FoxTunes.Interfaces;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for ProjectM.xaml
    /// </summary>
    [UIComponent("CB11A484-C9B5-4864-A0CD-59D433248AB4", role: UIComponentRole.Visualization)]
    public partial class ProjectM : UIComponentBase, IInvocableComponent
    {
        public const string CATEGORY = "17584DD4-2584-440A-9B6E-4A09EB924243";

        public const string PREVIOUS = "AAAA";

        public const string NEXT = "BBBB";

        public const string SEARCH = "CCCC";

        public const string PAUSE = "DDDD";

        static ProjectM()
        {
            Loader.Load("glew32.dll");
            Loader.Load("projectM-4.dll");
        }

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(ProjectM).Assembly.Location);
            }
        }

        public static IVisualizationDataSource VisualizationDataSource = ComponentRegistry.Instance.GetComponent<IVisualizationDataSource>();

        public static IUserInterface UserInterface = ComponentRegistry.Instance.GetComponent<IUserInterface>();

        public static IntPtr Context { get; private set; }

        public ProjectM()
        {
            this.InitializeComponent();
            this.CreateMenu();
            this.Data = new PCMVisualizationData()
            {
                Interval = TimeSpan.FromMilliseconds(16),
                Flags = VisualizationDataFlags.Individual
            };
            this.Buffers = new Dictionary<int, float[]>();
#if DEBUG
            this.Timer = new global::System.Timers.Timer();
            this.Timer.Interval = 1000;
            this.Timer.Elapsed += this.OnTimerElapsed;
            this.Timer.AutoReset = false;
            this.Timer.Start();
#endif
            this.Timer1 = new global::System.Timers.Timer();
            this.Timer1.Interval = 16;
            this.Timer1.Elapsed += this.OnTimer1Elapsed;
            this.Timer1.AutoReset = false;
            this.Timer1.Start();
            this.Timer2 = new global::System.Timers.Timer();
#if DEBUG
            this.Timer2.Interval = 15000;
#else
            this.Timer2.Interval = 60000;
#endif
            this.Timer2.Elapsed += this.OnTimer2Elapsed;
            this.Timer2.AutoReset = false;
            this.Timer2.Start();
        }

        public PCMVisualizationData Data { get; private set; }

        public IDictionary<int, float[]> Buffers { get; private set; }

#if DEBUG

        const int SLOW_THRESHOLD = 10;

        public global::System.Timers.Timer Timer { get; private set; }

        public volatile int Frames = 0;

        public int Slow = 0;

        protected virtual void OnTimerElapsed(object sender, EventArgs e)
        {
            try
            {
                this.FPS = Interlocked.Exchange(ref this.Frames, 0);
            }
            finally
            {
                this.Timer.Start();
            }
        }

        public int FPS
        {
            set
            {
                if (IntPtr.Zero.Equals(Context))
                {
                    return;
                }
                if (this.DownmixedData == null || this.DownmixedChannels == Channels.NONE)
                {
                    return;
                }
                if (value < SLOW_THRESHOLD)
                {
                    this.Slow++;
                    if (this.Slow > SLOW_THRESHOLD)
                    {
                        var fileName = Presets.FileNames[Presets.Index];
                        Logger.Write(this, LogLevel.Debug, "Slow down detected for preset: {0}", fileName);
                        var relativeFileName = GetRelativePath(Location, fileName);
                        var absoluteFileName = GetAbsolutePath(relativeFileName);
                        if (!string.IsNullOrEmpty(absoluteFileName))
                        {
                            File.Delete(absoluteFileName);
                        }
                        this.Slow = 0;
                    }
                }
                else
                {
                    this.Slow = 0;
                }
            }
        }

        public static string GetRelativePath(string basePath, string fullPath)
        {
            var baseUri = new Uri(AppendDirectorySeparatorChar(basePath));
            var fullUri = new Uri(fullPath);
            var relativeUri = baseUri.MakeRelativeUri(fullUri);
            var relativePath = Uri.UnescapeDataString(relativeUri.ToString());
            return relativePath.Replace('/', Path.DirectorySeparatorChar);
        }

        private static string AppendDirectorySeparatorChar(string path)
        {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return path + Path.DirectorySeparatorChar;
            return path;
        }

        private static string GetAbsolutePath(string relativePath)
        {
            var directoryName = Path.GetDirectoryName(Location);
            do
            {
                var absolutePath = Path.Combine(directoryName, "FoxTunes.UI.Windows.Visualizations.ProjectM", relativePath);
                if (File.Exists(absolutePath))
                {
                    return absolutePath;
                }
                directoryName = Path.GetDirectoryName(directoryName);
            } while (!string.IsNullOrEmpty(directoryName));
            return null;
        }

#endif

        public global::System.Timers.Timer Timer1 { get; private set; }

        public global::System.Timers.Timer Timer2 { get; private set; }

        public GLControl GLControl { get; private set; }

        public Channels DownmixedChannels { get; private set; }

        public float[] DownmixedData { get; private set; }

        public bool OwnsContext { get; private set; }

        protected virtual void CreateMenu()
        {
            var menu = new Menu()
            {
                Components = new ObservableCollection<IInvocableComponent>()
                {
                    this
                }
            };
            this.ContextMenu = menu;
        }

        protected virtual void OnTimer1Elapsed(object sender, global::System.Timers.ElapsedEventArgs e)
        {
            var task = Windows.Invoke(() =>
            {
                try
                {
                    if (IntPtr.Zero.Equals(Context))
                    {
                        return;
                    }
                    if (VisualizationDataSource.Update(this.Data))
                    {
                        switch (this.Data.Channels)
                        {
                            case 1:
                                this.DownmixedData = this.Data.Samples32;
                                this.DownmixedChannels = Channels.MONO;
                                break;
                            case 2:
                                this.DownmixedData = this.Data.Samples32;
                                this.DownmixedChannels = Channels.STEREO;
                                break;
                            case 6:
                                this.DownmixedData = Downmix51ToStereo(this.Data.Samples32);
                                this.DownmixedChannels = Channels.STEREO;
                                break;
                            case 8:
                                this.DownmixedData = Downmix71ToStereo(this.Data.Samples32);
                                this.DownmixedChannels = Channels.STEREO;
                                break;
                            default:
                                return;
                        }
                    }
                    else
                    {
                        this.DownmixedData = null;
                        this.DownmixedChannels = Channels.NONE;
                    }
                    if (this.GLControl == null)
                    {
                        return;
                    }
                    this.GLControl.Invalidate();
                }
                finally
                {
                    this.Timer1.Start();
                }
            });
        }

        protected virtual void OnTimer2Elapsed(object sender, global::System.Timers.ElapsedEventArgs e)
        {
            var task = Windows.Invoke(() =>
            {
                try
                {
                    if (IntPtr.Zero.Equals(Context))
                    {
                        return;
                    }
                    var fileName = Presets.Next();
                    Logger.Write(this, LogLevel.Debug, "Loading preset: {0}", fileName);
                    if (!string.IsNullOrEmpty(fileName))
                    {
                        projectm_load_preset_file(Context, fileName, true);
#if DEBUG
                        this.Slow = 0;
#endif
                    }
                }
                finally
                {
                    this.Timer2.Start();
                }
            });
        }

        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (this.GLControl != null)
            {
                //Nothing to do.
                return;
            }
            if (!IntPtr.Zero.Equals(Context))
            {
                UserInterface.Warn(Strings.ProjectM_InstanceWarning);
                return;
            }
            var window = this.FindAncestor<Window>();
            if (window != null)
            {
                if (window.AllowsTransparency)
                {
                    Logger.Write(this, LogLevel.Warn, "Transparency is enabled for window {0}, warning.", window.Title);
                    UserInterface.Warn(Strings.ProjectM_TransparencyWarning);
                    return;
                }
            }
            this.GLControl = new GLControl(new GraphicsMode(32, 24, 0, 4));
            this.GLControl.Dock = DockStyle.Fill;
            this.GLControl.Visible = true;
            this.GLControl.Load += this.OnLoad;
            this.GLControl.Paint += this.OnPaint;
            this.GLControl.HandleCreated += this.OnHandleCreated;
            this.GLControl.SizeChanged += this.OnSizeChanged;
            this.GLControl.MouseDown += this.OnMouseDown;
            Logger.Write(this, LogLevel.Debug, "GLControl created.");
            this.Host.Child = this.GLControl;
            this.Host.Visibility = Visibility.Visible;
            this.Host.UpdateLayout();
            this.GLControl.CreateControl();
            this.GLControl.Refresh();
            Logger.Write(this, LogLevel.Debug, "GLControl initialized.");
        }

        protected virtual void OnLoad(object sender, EventArgs e)
        {
            if (!this.GLControl.Context.IsCurrent)
            {
                this.GLControl.MakeCurrent();
            }
            Logger.Write(this, LogLevel.Debug, "Initializing glew.");
            var result = glewInit();
            if (result != 0)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to initialize glew: {0}", result);
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Glew initialized.");
            Logger.Write(this, LogLevel.Debug, "Creating ProjectM context.");
            Context = projectm_create();
            if (IntPtr.Zero.Equals(Context))
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create ProjectM context.");
                return;
            }
            this.OwnsContext = true;
            Logger.Write(this, LogLevel.Debug, "Created ProjectM context.");
            var directoryName = Path.Combine(Location, "Textures");
            Logger.Write(this, LogLevel.Debug, "Setting texture path: {0}", directoryName);
            projectm_set_texture_search_paths(Context, new[] { directoryName });
            var fileName = Presets.Next();
            Logger.Write(this, LogLevel.Debug, "Loading preset: {0}", fileName);
            if (!string.IsNullOrEmpty(fileName))
            {
                projectm_load_preset_file(Context, fileName, false);
#if DEBUG
                this.Slow = 0;
#endif
            }
        }

        protected virtual void OnPaint(object sender, PaintEventArgs e)
        {
            if (IntPtr.Zero.Equals(Context))
            {
                return;
            }
            if (this.DownmixedData != null && this.DownmixedChannels != Channels.NONE)
            {
                var channels = this.DownmixedChannels == Channels.MONO ? 1 : 2;
                var data = ResampleTo512(this.DownmixedData, channels);
                var frames = (uint)(data.Length / channels);
                projectm_pcm_add_float(Context, data, frames, this.DownmixedChannels);
                if (!this.GLControl.Context.IsCurrent)
                {
                    this.GLControl.MakeCurrent();
                }
                projectm_opengl_render_frame(Context);
                this.GLControl.SwapBuffers();
#if DEBUG
                Interlocked.Increment(ref this.Frames);
#endif
            }
            else
            {
                if (!this.GLControl.Context.IsCurrent)
                {
                    this.GLControl.MakeCurrent();
                }
                GL.ClearColor(0, 0, 0, 1); //Black.
                GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
                this.GLControl.SwapBuffers();
            }
        }

        protected virtual void OnHandleCreated(object sender, EventArgs e)
        {
            if (!this.GLControl.Context.IsCurrent)
            {
                this.GLControl.MakeCurrent();
            }
        }

        protected virtual void OnSizeChanged(object sender, EventArgs e)
        {
            Logger.Write(this, LogLevel.Debug, "GLControl size changed: {0}x{1}.", this.GLControl.Width, this.GLControl.Height);
            Logger.Write(this, LogLevel.Debug, "Updating GL viewport.");
            if (!this.GLControl.Context.IsCurrent)
            {
                this.GLControl.MakeCurrent();
            }
            GL.Viewport(0, 0, this.GLControl.Width, this.GLControl.Height);
            if (IntPtr.Zero.Equals(Context))
            {
                return;
            }
            Logger.Write(this, LogLevel.Debug, "Updating ProjectM window size.");
            projectm_set_window_size(Context, (UIntPtr)this.GLControl.Width, (UIntPtr)this.GLControl.Height);
        }

        protected virtual void OnMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                this.ContextMenu.IsOpen = true;
            }
        }

        public IEnumerable<string> InvocationCategories
        {
            get
            {
                yield return CATEGORY;
            }
        }

        public IEnumerable<IInvocationComponent> Invocations
        {
            get
            {
                yield return new InvocationComponent(CATEGORY, PREVIOUS, Strings.ProjectM_Previous);
                yield return new InvocationComponent(CATEGORY, NEXT, Strings.ProjectM_Next);
                yield return new InvocationComponent(CATEGORY, SEARCH, Strings.ProjectM_Search, attributes: InvocationComponent.ATTRIBUTE_SEPARATOR);
                yield return new InvocationComponent(CATEGORY, PAUSE, Strings.ProjectM_Pause, attributes: (byte)((this.Timer2.Enabled ? InvocationComponent.ATTRIBUTE_NONE : InvocationComponent.ATTRIBUTE_SELECTED) | InvocationComponent.ATTRIBUTE_SEPARATOR));
            }
        }

        public Task InvokeAsync(IInvocationComponent component)
        {
            switch (component.Id)
            {
                case PREVIOUS:
                    return this.Previous();
                case NEXT:
                    return this.Next();
                case SEARCH:
                    return this.Search();
                case PAUSE:
                    return this.Pause();

            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public Task Previous()
        {
            if (this.Timer2.Enabled)
            {
                this.Timer2.Stop();
                this.Timer2.Start();
            }
            return Windows.Invoke(() =>
            {
                if (IntPtr.Zero.Equals(Context))
                {
                    return;
                }
                var fileName = Presets.Previous();
                Logger.Write(this, LogLevel.Debug, "Loading preset: {0}", fileName);
                if (!string.IsNullOrEmpty(fileName))
                {
                    projectm_load_preset_file(Context, fileName, true);
#if DEBUG
                    this.Slow = 0;
#endif
                }
            });
        }

        public Task Next()
        {
            if (this.Timer2.Enabled)
            {
                this.Timer2.Stop();
                this.Timer2.Start();
            }
            return Windows.Invoke(() =>
            {
                if (IntPtr.Zero.Equals(Context))
                {
                    return;
                }
                var fileName = Presets.Next();
                Logger.Write(this, LogLevel.Debug, "Loading preset: {0}", fileName);
                if (!string.IsNullOrEmpty(fileName))
                {
                    projectm_load_preset_file(Context, fileName, true);
#if DEBUG
                    this.Slow = 0;
#endif
                }
            });
        }

        public Task Search()
        {
            if (IntPtr.Zero.Equals(Context))
            {
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
            Presets.Search = UserInterface.Prompt(Strings.ProjectM_Search, Presets.Search);
            return this.Next();
        }

        public Task Pause()
        {
            return Windows.Invoke(() =>
            {
                if (this.Timer2.Enabled)
                {
                    Logger.Write(this, LogLevel.Debug, "Pausing preset rotation.");
                    this.Timer2.Stop();
                }
                else
                {
                    Logger.Write(this, LogLevel.Debug, "Resuming preset rotation.");
                    this.Timer2.Start();
                }
            });
        }

        protected override void OnDisposing()
        {
#if DEBUG
            if (this.Timer != null)
            {
                this.Timer.Elapsed -= this.OnTimerElapsed;
                this.Timer.Dispose();
            }
#endif
            if (this.Timer1 != null)
            {
                this.Timer1.Elapsed -= this.OnTimer1Elapsed;
                this.Timer1.Dispose();
            }
            if (this.Timer2 != null)
            {
                this.Timer2.Elapsed -= this.OnTimer2Elapsed;
                this.Timer2.Dispose();
            }
            if (this.GLControl != null)
            {
                Logger.Write(this, LogLevel.Debug, "Disposing GLControl.");
                this.GLControl.Load -= this.OnLoad;
                this.GLControl.Paint -= this.OnPaint;
                this.GLControl.HandleCreated -= this.OnHandleCreated;
                this.GLControl.SizeChanged -= this.OnSizeChanged;
                this.GLControl.MouseDown -= this.OnMouseDown;
            }
            if (!IntPtr.Zero.Equals(Context) && this.OwnsContext)
            {
                Logger.Write(this, LogLevel.Debug, "Destroying ProjectM context.");
                projectm_destroy(Context);
                Context = IntPtr.Zero;
            }
            base.OnDisposing();
        }

        protected virtual float[] Downmix51ToStereo(float[] input)
        {
            var inChannels = 6;
            var outChannels = 2;
            var frames = input.Length / inChannels;
            var output = this.GetBuffer(frames * outChannels);
            for (var f = 0; f < frames; f++)
            {
                var i = f * inChannels;
                var o = f * outChannels;
                var FL = input[i + 0];
                var FR = input[i + 1];
                var C = input[i + 2];
                var LFE = input[i + 3];
                var SL = input[i + 4];
                var SR = input[i + 5];
                output[o + 0] = FL + 0.707f * C + 0.707f * SL + 0.5f * LFE;
                output[o + 1] = FR + 0.707f * C + 0.707f * SR + 0.5f * LFE;
            }
            return output;
        }

        protected virtual float[] Downmix71ToStereo(float[] input)
        {
            var inChannels = 8;
            var outChannels = 2;
            var frames = input.Length / inChannels;
            var output = this.GetBuffer(frames * outChannels);
            for (var f = 0; f < frames; f++)
            {
                var i = f * inChannels;
                var o = f * outChannels;
                var FL = input[i + 0];
                var FR = input[i + 1];
                var C = input[i + 2];
                var LFE = input[i + 3];
                var BL = input[i + 4];
                var BR = input[i + 5];
                var SL = input[i + 6];
                var SR = input[i + 7];
                output[o + 0] = FL + 0.707f * C + 0.707f * SL + 0.707f * BL + 0.5f * LFE;
                output[o + 1] = FR + 0.707f * C + 0.707f * SR + 0.707f * BR + 0.5f * LFE;
            }
            return output;
        }

        protected virtual float[] ResampleTo512(float[] input, int channels)
        {
            const int targetFrames = 512;
            var inputFrames = input.Length / channels;
            var output = this.GetBuffer(targetFrames * channels);
            for (var i = 0; i < targetFrames; i++)
            {
                var srcIndex = (float)i * inputFrames / targetFrames;
                var i0 = (int)srcIndex;
                var i1 = Math.Min(i0 + 1, inputFrames - 1);
                var t = srcIndex - i0;
                for (var c = 0; c < channels; c++)
                {
                    var a = input[(i0 * channels) + c];
                    var b = input[(i1 * channels) + c];
                    output[(i * channels) + c] = a + (b - a) * t;
                }
            }
            return output;
        }

        protected virtual float[] GetBuffer(int size)
        {
            return this.Buffers.GetOrAdd(size, () => new float[size]);
        }

        public static void projectm_set_texture_search_paths(IntPtr instance, string[] paths)
        {
            var _paths = new IntPtr[paths.Length];
            try
            {
                for (var a = 0; a < paths.Length; a++)
                {
                    _paths[a] = Marshal.StringToHGlobalAnsi(paths[a]);
                }
                projectm_set_texture_search_paths(instance, _paths, _paths.Length);
            }
            finally
            {
                foreach (var path in _paths)
                {
                    if (path != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(path);
                    }
                }
            }
        }


        [DllImport("glew32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int glewInit();

        [DllImport("projectM-4.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr projectm_create();

        [DllImport("projectM-4.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void projectm_set_texture_search_paths(IntPtr instance, IntPtr[] paths, int count);

        [DllImport("projectM-4.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void projectm_set_window_size(IntPtr ctx, UIntPtr width, UIntPtr height);

        [DllImport("projectM-4.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        public static extern void projectm_load_preset_file(IntPtr ctx, [MarshalAs(UnmanagedType.LPStr)] string filename, [MarshalAs(UnmanagedType.I1)] bool smooth_transition);

        [DllImport("projectM-4.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void projectm_pcm_add_float(IntPtr ctx, [In] float[] pcm, uint size, Channels channels);

        [DllImport("projectM-4.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void projectm_opengl_render_frame(IntPtr ctx);

        [DllImport("projectM-4.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void projectm_destroy(IntPtr ctx);

        public enum Channels : int
        {
            NONE = 0,
            MONO = 1,
            STEREO = 2
        }
    }
}