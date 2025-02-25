using FoxTunes.Interfaces;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace FoxTunes
{
    public static partial class WindowExtensions
    {
        private static readonly ConditionalWeakTable<Window, AllowsTransparencyBehaviour> AllowsTransparencyBehaviours = new ConditionalWeakTable<Window, AllowsTransparencyBehaviour>();

        public static readonly DependencyProperty AllowsTransparencyProperty = DependencyProperty.RegisterAttached(
            "AllowsTransparency",
            typeof(bool),
            typeof(WindowExtensions),
            new FrameworkPropertyMetadata(false, new PropertyChangedCallback(OnAllowsTransparencyPropertyChanged))
        );

        public static bool GetAllowsTransparency(Window source)
        {
            return (bool)source.GetValue(AllowsTransparencyProperty);
        }

        public static void SetAllowsTransparency(Window source, bool value)
        {
            source.SetValue(AllowsTransparencyProperty, value);
        }

        private static void OnAllowsTransparencyPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var window = sender as Window;
            if (window == null)
            {
                return;
            }
            if (GetAllowsTransparency(window))
            {
                var behaviour = default(AllowsTransparencyBehaviour);
                if (!AllowsTransparencyBehaviours.TryGetValue(window, out behaviour))
                {
                    AllowsTransparencyBehaviours.Add(window, new AllowsTransparencyBehaviour(window));
                }
            }
            else
            {
                AllowsTransparencyBehaviours.Remove(window);
            }
        }

        private class AllowsTransparencyBehaviour : UIBehaviour
        {
            public static bool Warned = false;

            private AllowsTransparencyBehaviour()
            {
                this.UserInterface = ComponentRegistry.Instance.GetComponent<IUserInterface>();
                this.Configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();
            }

            public AllowsTransparencyBehaviour(Window window) : this()
            {
                this.Window = window;
                if (this.Configuration != null)
                {
                    this.Configuration.GetElement<BooleanConfigurationElement>(
                        WindowsUserInterfaceConfiguration.SECTION,
                        WindowsUserInterfaceConfiguration.TRANSPARENCY
                    ).ConnectValue(value =>
                    {
                        this.Enabled = value;
                        if (this.Window != null)
                        {
                            this.EnableTransparency(value);
                        }
                    });
                }
            }

            public IUserInterface UserInterface { get; private set; }

            public IConfiguration Configuration { get; private set; }

            public bool Enabled { get; private set; }

            public Window Window { get; private set; }

            public virtual void EnableTransparency(bool enable)
            {
                if (this.Window.AllowsTransparency != enable)
                {
                    if (new WindowInteropHelper(this.Window).Handle != IntPtr.Zero)
                    {
                        if (!Warned)
                        {
                            this.UserInterface.Warn(Strings.WindowExtensions_TransparencyWarning);
                            Warned = true;
                        }
                        return;
                    }
                    this.Window.AllowsTransparency = enable;
                }
            }
        }

        public static Color DefaultAccentColor
        {
            get
            {
                var colors = new DwmColors();
                DwmGetColorizationParameters(ref colors);
                var color = Color.FromArgb(
                    (byte)((colors.ColorizationColor >> 24) & 0xff),
                    (byte)((colors.ColorizationColor >> 16) & 0xff),
                    (byte)((colors.ColorizationColor >> 8) & 0xff),
                    (byte)((colors.ColorizationColor >> 0) & 0xff)
                );
                return color;
            }
        }

        public static void EnableBlur(IntPtr handle)
        {
            var accent = new AccentPolicy()
            {
                AccentState = AccentState.ACCENT_ENABLE_BLURBEHIND
            };
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);
                var data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;
                SetWindowCompositionAttribute(handle, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        public static void EnableAcrylicBlur(IntPtr handle, Color color)
        {
            var accent = new AccentPolicy()
            {
                AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                GradientColor = (color.A << 24) + (color.B << 16) + (color.G << 8) + color.R
            };
            var accentStructSize = Marshal.SizeOf(accent);
            var accentPtr = Marshal.AllocHGlobal(accentStructSize);
            try
            {
                Marshal.StructureToPtr(accent, accentPtr, false);
                var data = new WindowCompositionAttributeData();
                data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;
                SetWindowCompositionAttribute(handle, ref data);
            }
            finally
            {
                Marshal.FreeHGlobal(accentPtr);
            }
        }

        public struct DwmColors
        {
            public uint ColorizationColor;

            public uint ColorizationAfterglow;

            public uint ColorizationColorBalance;

            public uint ColorizationAfterglowBalance;
            public uint ColorizationBlurBalance;
            public uint ColorizationGlassReflectionIntensity;
            public uint ColorizationOpaqueBlend;
        }

        [DllImport("dwmapi.dll", EntryPoint = "#127")]
        public static extern void DwmGetColorizationParameters(ref DwmColors colors);

        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);

        public enum AccentState
        {
            ACCENT_DISABLED = 1,
            ACCENT_ENABLE_GRADIENT = 0,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public WindowCompositionAttribute Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        public enum WindowCompositionAttribute
        {
            WCA_ACCENT_POLICY = 19
        }
    }
}
