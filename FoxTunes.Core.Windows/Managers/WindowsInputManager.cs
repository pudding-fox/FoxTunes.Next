using FoxTunes.Interfaces;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class WindowsInputManager : InputManager, IStandardManager
    {
        const int WH_KEYBOARD_LL = 13;

        public const int WM_KEYUP = 0x0101;

        public const int VK_ALT = 1;

        public const int VK_CONTROL = 2;

        public const int VK_SHIFT = 4;

        public const int VK_WINDOWS = 8;

        const int VK_LMENU = 0xA4;

        const int VK_RMENU = 0xA5;

        const int VK_LCONTROL = 0xA2;

        const int VK_RCONTROL = 0xA3;

        const int VK_LSHIFT = 0xA0;

        const int VK_RSHIFT = 0xA1;

        const int VK_LWIN = 0x5B;

        const int VK_RWIN = 0x5C;

        public WindowsInputManager()
        {
            this.Callback = (int code, IntPtr input, ref KeyboardInput keys) =>
            {
                if (code >= 0)
                {
                    switch ((int)input)
                    {
                        case WM_KEYUP:
                            var modifiers = this.GetModifiers();
                            var keyCode = keys.KeyCode;
                            this.Dispatch(() => this.OnInputEvent(modifiers, keyCode));
                            break;
                    }
                }
                return CallNextHookEx(this.Hook, code, input, ref keys);
            };
        }

        public IntPtr Hook { get; private set; }

        public KeyboardProc Callback { get; private set; }

        protected override void OnEnabledChanged()
        {
            if (this.Enabled)
            {
                this.Enable();
            }
            else
            {
                this.Disable();
            }
            base.OnEnabledChanged();
        }

        protected virtual int GetModifiers()
        {
            var modifiers = 0;
            if (IsKeyPressed(VK_LMENU) || IsKeyPressed(VK_RMENU))
            {
                modifiers |= VK_ALT;
            }
            if (IsKeyPressed(VK_LCONTROL) || IsKeyPressed(VK_RCONTROL))
            {
                modifiers |= VK_CONTROL;
            }
            if (IsKeyPressed(VK_LSHIFT) || IsKeyPressed(VK_RSHIFT))
            {
                modifiers |= VK_SHIFT;
            }
            if (IsKeyPressed(VK_LWIN) || IsKeyPressed(VK_RWIN))
            {
                modifiers |= VK_WINDOWS;
            }
            return modifiers;
        }

        protected virtual bool IsKeyPressed(int key)
        {
            //What the fuck?
            return (GetKeyState(key) & 0x8000) != 0;
        }

        protected virtual void Enable()
        {
            if (this.Hook == IntPtr.Zero)
            {
                this.Hook = SetHook(this.Callback);
                if (this.Hook != IntPtr.Zero)
                {
                    Logger.Write(this, LogLevel.Debug, "Added keyboard hook, global keyboard shortcuts are enabled.");
                }
                else
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to add keyboard hook, global keyboard shortcuts are disabled.");
                }
            }
        }

        protected virtual void Disable()
        {
            if (this.Hook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(this.Hook);
                this.Hook = IntPtr.Zero;
                Logger.Write(this, LogLevel.Debug, "Removed keyboard hook, global keyboard shortcuts are disabled.");
            }
        }

        protected override void OnDisposing()
        {
            this.Disable();
            base.OnDisposing();
        }

        public delegate IntPtr KeyboardProc(int code, IntPtr input, ref KeyboardInput keys);

        public static IntPtr SetHook(KeyboardProc proc)
        {
            using (var process = Process.GetCurrentProcess())
            {
                using (var module = process.MainModule)
                {
                    return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(module.ModuleName), 0);
                }
            }
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int id, KeyboardProc proc, IntPtr mod, uint thread);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hook);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr input, ref KeyboardInput keys);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string module);

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern short GetKeyState(int code);

        public struct KeyboardInput
        {
            public int KeyCode;

            public int ScanCode;

            public int Flags;

            public int Time;

            public int Info;
        }
    }
}