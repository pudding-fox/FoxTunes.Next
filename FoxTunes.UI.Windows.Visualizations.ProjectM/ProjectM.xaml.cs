using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;

namespace FoxTunes
{
    /// <summary>
    /// Interaction logic for ProjectM.xaml
    /// </summary>
    [UIComponent("CB11A484-C9B5-4864-A0CD-59D433248AB4", role: UIComponentRole.Visualization)]
    public partial class ProjectM : UIComponentBase
    {
        static ProjectM()
        {
            Loader.Load("glew32.dll");
            Loader.Load("projectM-4.dll");
        }

        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(AssemblyResolver).Assembly.Location);
            }
        }

        public ProjectM()
        {
            this.InitializeComponent();
        }

        public GLControl GLControl { get; private set; }

        public IntPtr Context { get; private set; }

        protected virtual void OnLoaded(object sender, RoutedEventArgs e)
        {
            this.GLControl = new GLControl(new GraphicsMode(32, 24, 0, 4));
            this.GLControl.Dock = DockStyle.Fill;
            this.GLControl.Load += this.OnLoad;
            this.GLControl.Paint += this.OnPaint;
            this.Host.Child = this.GLControl;
            this.GLControl.CreateControl();
        }

        protected virtual void OnLoad(object sender, EventArgs e)
        {
            GL.ClearColor(1, 0, 0, 1); // bright red
            GL.Clear(ClearBufferMask.ColorBufferBit);
            this.GLControl.MakeCurrent();
            var result = glewInit();
            if (result != 0)
            {
                return;
            }
            this.Context = projectm_create();
            projectm_set_window_size(this.Context, (UIntPtr)this.GLControl.Width, (UIntPtr)this.GLControl.Height);
            projectm_load_preset_file(this.Context, Path.Combine(Location, "Presets\\Dancer\\Aurora\\$$$ Royal - Mashup (257).milk"), false);
        }

        protected virtual void OnPaint(object sender, PaintEventArgs e)
        {
            if (IntPtr.Zero.Equals(this.Context))
            {
                return;
            }
            GL.ClearColor(1, 0, 0, 1); // bright red
            GL.Clear(ClearBufferMask.ColorBufferBit);
            GL.Viewport(0, 0, this.GLControl.Width, this.GLControl.Height);
            float[] pcm = new float[512];
            for (int i = 0; i < pcm.Length; i++)
            {
                pcm[i] = (float)Math.Sin(i * 0.1f);
            }
            projectm_pcm_add_float(this.Context, pcm, Convert.ToUInt32(pcm.Length), Channels.STEREO);
            projectm_opengl_render_frame(this.Context);
            this.GLControl.SwapBuffers();
            this.GLControl.Invalidate();
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
        public static extern IntPtr projectm_destroy(IntPtr ctx);

        public enum Channels : int
        {
            MONO = 1,
            STEREO = 2
        }
    }
}