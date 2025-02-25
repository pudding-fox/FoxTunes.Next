using FoxTunes.Interfaces;
using System;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    public class WindowsMessageSink : MessageSink
    {
        public static readonly string ID = Guid.NewGuid().ToString("d");

        private static readonly int WM_TASKBARCREATED;

        static WindowsMessageSink()
        {
            try
            {
                WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");
            }
            catch
            {
                Logger.Write(typeof(WindowsMessageSink), LogLevel.Warn, "Failed to register window message: TaskbarCreated");
            }
        }

        const int MOUSE_MOVE = 0x200;

        const int MOUSE_LEFT_DOWN = 0x201;

        const int MOUSE_LEFT_UP = 0x202;

        const int MOUSE_DOUBLE_CLICK = 0x203;

        const int MOUSE_RIGHT_DOWN = 0x204;

        const int MOUSE_RIGHT_UP = 0x205;

        public WindowsMessageSink(uint id)
        {
            this.Callback = (IntPtr hwnd, uint uMsg, IntPtr wparam, IntPtr lparam) =>
            {
                if (uMsg == id)
                {
                    switch ((int)lparam)
                    {
                        case MOUSE_MOVE:
                            this.OnMouseMove();
                            break;
                        case MOUSE_LEFT_DOWN:
                            this.OnMouseLeftButtonDown();
                            break;
                        case MOUSE_LEFT_UP:
                            this.OnMouseLeftButtonUp();
                            break;
                        case MOUSE_DOUBLE_CLICK:
                            this.OnMouseDoubleClick();
                            break;
                        case MOUSE_RIGHT_DOWN:
                            this.OnMouseRightButtonDown();
                            break;
                        case MOUSE_RIGHT_UP:
                            this.OnMouseRightButtonUp();
                            break;
                    }
                }
                else if (uMsg == WM_TASKBARCREATED)
                {
                    this.OnTaskBarCreated();
                }
                return DefWindowProc(hwnd, uMsg, wparam, lparam);
            };
            var windowClass = WindowClass.Create(ID, this.Callback);
            RegisterClass(ref windowClass);
            this.Handle = CreateWindowEx(0, ID, string.Empty, 0, 0, 0, 1, 1, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }

        public override IntPtr Handle { get; protected set; }

        public WindowProcedureHandler Callback { get; private set; }

        protected override void OnDisposing()
        {
            if (this.Handle != IntPtr.Zero)
            {
                DestroyWindow(this.Handle);
                this.Handle = IntPtr.Zero;
            }
            base.OnDisposing();
        }

        public delegate IntPtr WindowProcedureHandler(IntPtr hwnd, uint uMsg, IntPtr wparam, IntPtr lparam);

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowClass
        {
            public uint style;

            public WindowProcedureHandler lpfnWndProc;

            public int cbClsExtra;

            public int cbWndExtra;

            public IntPtr hInstance;

            public IntPtr hIcon;

            public IntPtr hCursor;

            public IntPtr hbrBackground;

            [MarshalAs(UnmanagedType.LPWStr)]

            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]

            public string lpszClassName;

            public static WindowClass Create(string id, WindowProcedureHandler callback)
            {
                return new WindowClass()
                {
                    style = 0,
                    lpfnWndProc = callback,
                    cbClsExtra = 0,
                    cbWndExtra = 0,
                    hInstance = IntPtr.Zero,
                    hIcon = IntPtr.Zero,
                    hCursor = IntPtr.Zero,
                    hbrBackground = IntPtr.Zero,
                    lpszMenuName = "",
                    lpszClassName = id
                };
            }
        }

        [DllImport("user32.dll", EntryPoint = "CreateWindowExW", SetLastError = true)]
        public static extern IntPtr CreateWindowEx(int dwExStyle, [MarshalAs(UnmanagedType.LPWStr)] string lpClassName, [MarshalAs(UnmanagedType.LPWStr)] string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", EntryPoint = "RegisterClassW", SetLastError = true)]
        public static extern short RegisterClass(ref WindowClass lpWndClass);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wparam, IntPtr lparam);

        [DllImport("user32.dll")]
        public static extern int RegisterWindowMessage(string msg);
    }
}
