using FoxTunes.Interfaces;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

        public const string PAUSE = "CCCC";

        static ProjectM()
        {
            Loader.Load("glew32.dll");
            Loader.Load("projectM-4.dll");
        }

        public static IVisualizationDataSource VisualizationDataSource = ComponentRegistry.Instance.GetComponent<IVisualizationDataSource>();

        public ProjectM()
        {
            this.InitializeComponent();
            this.CreateMenu();
            this.Data = new PCMVisualizationData()
            {
                Interval = TimeSpan.FromMilliseconds(16),
                Flags = VisualizationDataFlags.Individual
            };
            this.Timer1 = new global::System.Timers.Timer();
            this.Timer1.Interval = 16;
            this.Timer1.Elapsed += this.OnTimer1Elapsed;
            this.Timer1.Start();
            this.Timer2 = new global::System.Timers.Timer();
            this.Timer2.Interval = 60000;
            this.Timer2.Elapsed += this.OnTimer2Elapsed;
            this.Timer2.Start();
        }

        public PCMVisualizationData Data { get; private set; }

        public global::System.Timers.Timer Timer1 { get; private set; }

        public global::System.Timers.Timer Timer2 { get; private set; }

        public GLControl GLControl { get; private set; }

        public IntPtr Context { get; private set; }

        public Channels DownmixedChannels { get; private set; }

        public float[] DownmixedData { get; private set; }

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
            });
        }

        protected virtual void OnTimer2Elapsed(object sender, global::System.Timers.ElapsedEventArgs e)
        {
            var task = Windows.Invoke(() =>
            {
                if (IntPtr.Zero.Equals(this.Context))
                {
                    return;
                }
                projectm_load_preset_file(this.Context, Presets.Next(), true);
            });
        }

        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.GLControl = new GLControl(new GraphicsMode(32, 24, 0, 4), 4, 6, GraphicsContextFlags.Default);
            this.GLControl.Dock = DockStyle.Fill;
            this.GLControl.Visible = true;
            this.GLControl.Load += this.OnLoad;
            this.GLControl.Paint += this.OnPaint;
            this.GLControl.HandleCreated += this.OnHandleCreated;
            this.GLControl.SizeChanged += this.OnSizeChanged;
            this.GLControl.MouseDown += this.OnMouseDown;
            this.Host.Child = this.GLControl;
            this.Host.Visibility = Visibility.Visible;
            this.Host.UpdateLayout();
            this.GLControl.CreateControl();
            this.GLControl.Refresh();
        }

        protected virtual void OnLoad(object sender, EventArgs e)
        {
            this.GLControl.MakeCurrent();
            var result = glewInit();
            if (result != 0)
            {
                return;
            }
            this.Context = projectm_create();
            var fileName = Presets.Next();
            projectm_load_preset_file(this.Context, fileName, false);
        }

        protected virtual void OnPaint(object sender, PaintEventArgs e)
        {
            if (IntPtr.Zero.Equals(this.Context))
            {
                return;
            }
            if (this.DownmixedData != null && this.DownmixedChannels != Channels.NONE)
            {
                var channels = this.DownmixedChannels == Channels.MONO ? 1 : 2;
                var data = ResampleTo512(this.DownmixedData, channels);
                var frames = (uint)(data.Length / channels);
                projectm_pcm_add_float(this.Context, data, frames, this.DownmixedChannels);
                this.GLControl.MakeCurrent();
                projectm_opengl_render_frame(this.Context);
                this.GLControl.SwapBuffers();
            }
        }

        protected virtual void OnHandleCreated(object sender, EventArgs e)
        {
            this.GLControl.MakeCurrent();
        }

        protected virtual void OnSizeChanged(object sender, EventArgs e)
        {
            this.GLControl.MakeCurrent();
            GL.Viewport(0, 0, this.GLControl.Width, this.GLControl.Height);
            if (IntPtr.Zero.Equals(this.Context))
            {
                return;
            }
            projectm_set_window_size(this.Context, (UIntPtr)this.GLControl.Width, (UIntPtr)this.GLControl.Height);
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
                if (IntPtr.Zero.Equals(this.Context))
                {
                    return;
                }
                var fileName = Presets.Previous();
                projectm_load_preset_file(this.Context, fileName, true);
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
                if (IntPtr.Zero.Equals(this.Context))
                {
                    return;
                }
                var fileName = Presets.Next();
                projectm_load_preset_file(this.Context, fileName, true);
            });
        }

        public Task Pause()
        {
            return Windows.Invoke(() =>
            {
                if (this.Timer2.Enabled)
                {
                    this.Timer2.Stop();
                }
                else
                {
                    this.Timer2.Start();
                }
            });
        }

        protected override void OnDisposing()
        {
            if (!IntPtr.Zero.Equals(this.Context))
            {
                projectm_destroy(this.Context);
                this.Context = IntPtr.Zero;
            }
            base.OnDisposing();
        }

        public static float[] Downmix51ToStereo(float[] input)
        {
            var inChannels = 6;
            var outChannels = 2;
            var frames = input.Length / inChannels;
            var output = new float[frames * outChannels];
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

        public static float[] Downmix71ToStereo(float[] input)
        {
            var inChannels = 8;
            var outChannels = 2;
            var frames = input.Length / inChannels;
            var output = new float[frames * outChannels];
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

        public static float[] ResampleTo512(float[] input, int channels)
        {
            const int targetFrames = 512;

            int inputFrames = input.Length / channels;
            if (inputFrames == 0) return new float[targetFrames * channels];

            float[] output = new float[targetFrames * channels];

            for (int i = 0; i < targetFrames; i++)
            {
                float srcIndex = (float)i * inputFrames / targetFrames;
                int i0 = (int)srcIndex;
                int i1 = Math.Min(i0 + 1, inputFrames - 1);
                float t = srcIndex - i0;

                for (int c = 0; c < channels; c++)
                {
                    float a = input[(i0 * channels) + c];
                    float b = input[(i1 * channels) + c];

                    output[(i * channels) + c] = a + (b - a) * t;
                }
            }

            return output;
        }


        [DllImport("glew32.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int glewInit();

        [DllImport("projectM-4.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr projectm_create();

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