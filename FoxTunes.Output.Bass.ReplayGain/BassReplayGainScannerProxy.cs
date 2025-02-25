using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace FoxTunes
{
    public class BassScannerProxy : BaseComponent, IBassReplayGainScanner
    {
        const int TIMEOUT = 10000;

        public static readonly object ReadSyncRoot = new object();

        public static readonly object WriteSyncRoot = new object();

        private BassScannerProxy()
        {
            this.TerminateCallback = new DelayedCallback(this.Terminate, TimeSpan.FromMilliseconds(TIMEOUT));
        }

        public BassScannerProxy(Process process, IEnumerable<ScannerItem> scannerItems) : this()
        {
            this.Process = process;
            this.ScannerItems = scannerItems;
        }

        public DelayedCallback TerminateCallback { get; private set; }

        public Process Process { get; private set; }

        public IEnumerable<ScannerItem> ScannerItems { get; private set; }

        public bool IsCancelling { get; private set; }

        public bool IsComplete { get; private set; }

        public void Scan()
        {
            Logger.Write(this, LogLevel.Debug, "Sending {0} items to scanner container process.", this.ScannerItems.Count());
            this.Send(this.ScannerItems.ToArray());
            Logger.Write(this, LogLevel.Debug, "Waiting for scanner container process to complete.");
            this.Process.WaitForExit();
            this.TerminateCallback.Disable();
            if (this.Process.ExitCode != 0)
            {
                if (this.ScannerItems != null)
                {
                    foreach (var scannerItem in this.ScannerItems)
                    {
                        if (scannerItem.Status != ScannerItemStatus.None)
                        {
                            continue;
                        }
                        scannerItem.Status = ScannerItemStatus.Failed;
                    }
                }
                throw new InvalidOperationException(string.Format("Process does not indicate success: Code = {0}", this.Process.ExitCode));
            }
        }

        public void Update()
        {
            var value = this.Recieve();
            if (value == null)
            {
                return;
            }
            if (value is ScannerStatus)
            {
                this.UpdateStatus(value as ScannerStatus);
            }
            else if (value is IEnumerable<ScannerItem>)
            {
                this.UpdateItems(value as IEnumerable<ScannerItem>);
            }
        }

        public void Cancel()
        {
            Logger.Write(this, LogLevel.Debug, "Sending cancel command to scanner container process.");
            this.Send(new ScannerCommand(ScannerCommandType.Cancel));
            this.Process.StandardInput.Close();
            this.TerminateCallback.Enable();
        }

        public void Quit()
        {
            Logger.Write(this, LogLevel.Debug, "Sending quit command to scanner container process.");
            this.Send(new ScannerCommand(ScannerCommandType.Quit));
            this.Process.StandardInput.Close();
            this.TerminateCallback.Enable();
        }

        protected virtual void Terminate()
        {
            try
            {
                if (this.Process.HasExited)
                {
                    return;
                }
                Logger.Write(this, LogLevel.Warn, "Scanner container process did not exit after {0}ms, terminating it.", TIMEOUT);
                this.Process.Kill();
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to terminate scanner container process: {0}", e.Message);
            }
        }

        protected virtual void Send(object value)
        {
            lock (WriteSyncRoot)
            {
                try
                {
                    if (this.Process.StandardInput.BaseStream != null && this.Process.StandardInput.BaseStream.CanWrite)
                    {
                        Serializer.Instance.Write(this.Process.StandardInput.BaseStream, value);
                        this.Process.StandardInput.Flush();
                    }
                }
                catch
                {
                    //Nothing can be done.
                }
            }
        }

        protected virtual object Recieve()
        {
            lock (ReadSyncRoot)
            {
                try
                {
                    if (this.Process.StandardOutput.BaseStream != null && this.Process.StandardOutput.BaseStream.CanRead)
                    {
                        var value = Serializer.Instance.Read(this.Process.StandardOutput.BaseStream);
                        return value;
                    }
                }
                catch
                {
                    //Nothing can be done.
                }
            }
            return null;
        }

        protected virtual void UpdateStatus(ScannerStatus status)
        {
            Logger.Write(this, LogLevel.Debug, "Recieved status from scanner container process: {0}", Enum.GetName(typeof(ScannerStatusType), status.Type));
            switch (status.Type)
            {
                case ScannerStatusType.Complete:
                case ScannerStatusType.Error:
                    Logger.Write(this, LogLevel.Debug, "Fetching final status and shutting down scanner container process.");
                    this.Update();
                    this.Quit();
                    this.Process.StandardInput.Close();
                    this.Process.StandardOutput.Close();
                    break;
            }
        }

        protected virtual void UpdateItems(IEnumerable<ScannerItem> scannerItems)
        {
            this.ScannerItems = scannerItems;
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
            if (this.Process != null)
            {
                if (!this.Process.HasExited)
                {
                    Logger.Write(this, LogLevel.Warn, "Process is incomplete.");
                    this.Process.Kill();
                }
                this.Process.Dispose();
            }
        }

        ~BassScannerProxy()
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
}
