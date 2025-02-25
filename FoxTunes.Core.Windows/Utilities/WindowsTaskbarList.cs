﻿using System;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    public static class WindowsTaskbarList
    {
        public const int WM_COMMAND = 0x0111;

        public const int THBN_CLICKED = 0x1800;

        private static readonly object SyncRoot = new object();

        private static ITaskbarList4 _Instance;

        public static ITaskbarList4 Instance
        {
            get
            {
                if (_Instance == null)
                {
                    lock (SyncRoot)
                    {
                        if (_Instance == null)
                        {
                            _Instance = (ITaskbarList4)new TaskbarList();
                            _Instance.HrInit();
                        }
                    }
                }
                return _Instance;
            }
        }

        public enum HResult
        {
            Ok = 0x0000,
            False = 0x0001,
            InvalidArguments = unchecked((int)0x80070057),
            OutOfMemory = unchecked((int)0x8007000E),
            NoInterface = unchecked((int)0x80004002),
            Fail = unchecked((int)0x80004005),
            ElementNotFound = unchecked((int)0x80070490),
            TypeElementNotFound = unchecked((int)0x8002802B),
            NoObject = unchecked((int)0x800401E5),
            Win32ErrorCanceled = 1223,
            Canceled = unchecked((int)0x800704C7),
            ResourceInUse = unchecked((int)0x800700AA),
            AccessDenied = unchecked((int)0x80030005)
        }

        public enum TaskbarProgressBarStatus
        {
            NoProgress = 0,
            Indeterminate = 0x1,
            Normal = 0x2,
            Error = 0x4,
            Paused = 0x8
        }

        public enum ThumbButtonMask
        {
            Bitmap = 0x1,
            Icon = 0x2,
            Tooltip = 0x4,
            THB_FLAGS = 0x8
        }

        [Flags]
        public enum ThumbButtonOptions
        {
            Enabled = 0x00000000,
            Disabled = 0x00000001,
            DismissOnClick = 0x00000002,
            NoBackground = 0x00000004,
            Hidden = 0x00000008,
            NonInteractive = 0x00000010
        }

        public enum SetTabPropertiesOption
        {
            None = 0x0,
            UseAppThumbnailAlways = 0x1,
            UseAppThumbnailWhenActive = 0x2,
            UseAppPeekAlways = 0x4,
            UseAppPeekWhenActive = 0x8
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct ThumbButton
        {
            public const int Clicked = 0x1800;

            [MarshalAs(UnmanagedType.U4)]
            public ThumbButtonMask Mask;

            public uint Id;

            public uint Bitmap;

            public IntPtr Icon;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string Tip;

            [MarshalAs(UnmanagedType.U4)]
            public ThumbButtonOptions Flags;
        }

        [ComImportAttribute()]
        [GuidAttribute("c43dc798-95d1-4bea-9030-bb99e2983a1a")]
        [InterfaceTypeAttribute(ComInterfaceType.InterfaceIsIUnknown)]
        public interface ITaskbarList4
        {
            // ITaskbarList
            [PreserveSig]
            void HrInit();

            [PreserveSig]
            void AddTab(IntPtr hwnd);

            [PreserveSig]
            void DeleteTab(IntPtr hwnd);

            [PreserveSig]
            void ActivateTab(IntPtr hwnd);

            [PreserveSig]
            void SetActiveAlt(IntPtr hwnd);

            // ITaskbarList2
            [PreserveSig]
            void MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fFullscreen);

            // ITaskbarList3
            [PreserveSig]
            HResult SetProgressValue(IntPtr hwnd, UInt64 ullCompleted, UInt64 ullTotal);

            [PreserveSig]
            HResult SetProgressState(IntPtr hwnd, TaskbarProgressBarStatus tbpFlags);

            [PreserveSig]
            void RegisterTab(IntPtr hwndTab, IntPtr hwndMDI);

            [PreserveSig]
            void UnregisterTab(IntPtr hwndTab);

            [PreserveSig]
            void SetTabOrder(IntPtr hwndTab, IntPtr hwndInsertBefore);

            [PreserveSig]
            void SetTabActive(IntPtr hwndTab, IntPtr hwndInsertBefore, uint dwReserved);

            [PreserveSig]
            HResult ThumbBarAddButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] ThumbButton[] pButtons);

            [PreserveSig]
            HResult ThumbBarUpdateButtons(IntPtr hwnd, uint cButtons, [MarshalAs(UnmanagedType.LPArray)] ThumbButton[] pButtons);

            [PreserveSig]
            HResult ThumbBarSetImageList(IntPtr hwnd, IntPtr himl);

            [PreserveSig]
            void SetOverlayIcon(IntPtr hwnd, IntPtr hIcon, [MarshalAs(UnmanagedType.LPWStr)] string pszDescription);

            [PreserveSig]
            void SetThumbnailTooltip(IntPtr hwnd, [MarshalAs(UnmanagedType.LPWStr)] string pszTip);

            [PreserveSig]
            void SetThumbnailClip(IntPtr hwnd, IntPtr prcClip);

            // ITaskbarList4
            void SetTabProperties(IntPtr hwndTab, SetTabPropertiesOption stpFlags);
        }

        [Guid("56FDF344-FD6D-11d0-958A-006097C9A090")]
        [ClassInterface(ClassInterfaceType.None)]
        [ComImport]
        public class TaskbarList
        {
        }
    }
}