using FoxTunes.Interfaces;
using Microsoft.Win32;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;

namespace FoxTunes
{
    public class RegistryMonitor : BaseComponent, IDisposable
    {
        public RegistryMonitor()
        {
            this.Event = new ManualResetEvent(false);
            this.Filter = RegChangeNotifyFilter.Key | RegChangeNotifyFilter.Attribute | RegChangeNotifyFilter.Value | RegChangeNotifyFilter.Security;
        }

        public IntPtr Hive { get; private set; }

        public string Name { get; private set; }

        public Thread Thread { get; private set; }

        public ManualResetEvent Event { get; private set; }

        public RegChangeNotifyFilter Filter { get; private set; }

        public RegistryMonitor(RegistryHive registryHive, string subKey)
        {
            this.InitRegistryKey(registryHive, subKey);
        }

        public RegChangeNotifyFilter RegChangeNotifyFilter { get; set; }

        private void InitRegistryKey(RegistryHive hive, string name)
        {
            switch (hive)
            {
                case RegistryHive.ClassesRoot:
                    this.Hive = HKEY_CLASSES_ROOT;
                    break;
                case RegistryHive.CurrentConfig:
                    this.Hive = HKEY_CURRENT_CONFIG;
                    break;
                case RegistryHive.CurrentUser:
                    this.Hive = HKEY_CURRENT_USER;
                    break;
                case RegistryHive.DynData:
                    this.Hive = HKEY_DYN_DATA;
                    break;
                case RegistryHive.LocalMachine:
                    this.Hive = HKEY_LOCAL_MACHINE;
                    break;
                case RegistryHive.PerformanceData:
                    this.Hive = HKEY_PERFORMANCE_DATA;
                    break;
                case RegistryHive.Users:
                    this.Hive = HKEY_USERS;
                    break;
            }
            this.Name = name;
        }

        public bool IsMonitoring
        {
            get
            {
                return this.Thread != null;
            }
        }

        public void Start()
        {
            if (this.IsMonitoring)
            {
                return;
            }
            this.Event.Reset();
            this.Thread = new Thread(new ThreadStart(this.Monitor));
            this.Thread.IsBackground = true;
            this.Thread.Start();
        }

        public void Stop()
        {
            if (this.Thread != null)
            {
                this.Event.Set();
                this.Thread.Join();
            }
        }

        private void Monitor()
        {
            try
            {
                var registryKey = default(IntPtr);
                {
                    int result = RegOpenKeyEx(
                        this.Hive,
                        this.Name,
                        0,
                        STANDARD_RIGHTS_READ | KEY_QUERY_VALUE | KEY_NOTIFY,
                        out registryKey
                    );
                    if (result != 0)
                    {
                        throw new Win32Exception(result);
                    }
                }

                try
                {
                    var notifyEvent = new AutoResetEvent(false);
                    var waitHandles = new WaitHandle[] { notifyEvent, this.Event };
                    while (!this.Event.WaitOne(0, true))
                    {
#if NET40
                        var handle = notifyEvent.SafeWaitHandle.DangerousGetHandle();
#else
                        var handle = notifyEvent.GetSafeWaitHandle().DangerousGetHandle();
#endif
                        var result = RegNotifyChangeKeyValue(registryKey, true, this.Filter, handle, true);
                        if (result != 0)
                        {
                            throw new Win32Exception(result);
                        }
                        if (WaitHandle.WaitAny(waitHandles) == 0)
                        {
                            this.OnChanged();
                        }
                    }
                }
                finally
                {
                    if (registryKey != IntPtr.Zero)
                    {
                        RegCloseKey(registryKey);
                    }
                }
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
            this.Thread = null;
        }

        public event EventHandler Changed;

        protected virtual void OnChanged()
        {
            if (this.Changed == null)
            {
                return;
            }
            this.Changed(this, EventArgs.Empty);
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
            this.Stop();
        }

        ~RegistryMonitor()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegOpenKeyEx(IntPtr hKey, string subKey, uint options, int samDesired, out IntPtr phkResult);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegNotifyChangeKeyValue(IntPtr hKey, bool bWatchSubtree, RegChangeNotifyFilter dwNotifyFilter, IntPtr hEvent, bool fAsynchronous);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern int RegCloseKey(IntPtr hKey);

        public const int KEY_QUERY_VALUE = 0x0001;

        public const int KEY_NOTIFY = 0x0010;

        public const int STANDARD_RIGHTS_READ = 0x00020000;

        public static readonly IntPtr HKEY_CLASSES_ROOT = new IntPtr(unchecked((int)0x80000000));

        public static readonly IntPtr HKEY_CURRENT_USER = new IntPtr(unchecked((int)0x80000001));

        public static readonly IntPtr HKEY_LOCAL_MACHINE = new IntPtr(unchecked((int)0x80000002));

        public static readonly IntPtr HKEY_USERS = new IntPtr(unchecked((int)0x80000003));

        public static readonly IntPtr HKEY_PERFORMANCE_DATA = new IntPtr(unchecked((int)0x80000004));

        public static readonly IntPtr HKEY_CURRENT_CONFIG = new IntPtr(unchecked((int)0x80000005));

        public static readonly IntPtr HKEY_DYN_DATA = new IntPtr(unchecked((int)0x80000006));
    }

    [Flags]
    public enum RegChangeNotifyFilter
    {
        Key = 1,
        Attribute = 2,
        Value = 4,
        Security = 8,
    }
}
