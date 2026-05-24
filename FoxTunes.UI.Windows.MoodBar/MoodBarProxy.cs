using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class MoodBarProxy : BaseComponent, IMoodBar
    {
        const int TIMEOUT = 10000;

        public static readonly object ReadSyncRoot = new object();

        public static readonly object WriteSyncRoot = new object();

        private MoodBarProxy()
        {
            this.TerminateCallback = new DelayedCallback(this.Terminate, TimeSpan.FromMilliseconds(TIMEOUT));
        }

        public MoodBarProxy(Process process, IEnumerable<MoodBarItem> MoodBarItems) : this()
        {
            this.Process = process;
            this.MoodBarItems = MoodBarItems;
        }

        public DelayedCallback TerminateCallback { get; private set; }

        public Process Process { get; private set; }

        public IEnumerable<MoodBarItem> MoodBarItems { get; private set; }

        public bool IsCancelling { get; private set; }

        public bool IsComplete { get; private set; }

        public Task Create()
        {
            Logger.Write(this, LogLevel.Debug, "Sending {0} items to moodbar container process.", this.MoodBarItems.Count());
            this.Send(this.MoodBarItems.ToArray());
            Logger.Write(this, LogLevel.Debug, "Waiting for moodbar container process to complete.");
            this.Process.WaitForExit();
            this.TerminateCallback.Disable();
            if (this.Process.ExitCode != 0)
            {
                if (this.MoodBarItems != null)
                {
                    foreach (var moodBarItem in this.MoodBarItems)
                    {
                        if (moodBarItem.Status != MoodBarItemStatus.None)
                        {
                            continue;
                        }
                        moodBarItem.Status = MoodBarItemStatus.Failed;
                    }
                }
                throw new InvalidOperationException(string.Format("Process does not indicate success: Code = {0}", this.Process.ExitCode));
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        public void Update()
        {
            var value = this.Recieve();
            if (value == null)
            {
                return;
            }
            if (value is MoodBarStatus)
            {
                this.UpdateStatus(value as MoodBarStatus);
            }
            else if (value is IEnumerable<MoodBarItem>)
            {
                this.UpdateItems(value as IEnumerable<MoodBarItem>);
            }
            this.OnUpdated();
        }

        public void Cancel()
        {
            Logger.Write(this, LogLevel.Debug, "Sending cancel command to MoodBar container process.");
            this.Send(new MoodBarCommand(MoodBarCommandType.Cancel));
            this.Process.StandardInput.Close();
            this.TerminateCallback.Enable();
        }

        public void Quit()
        {
            Logger.Write(this, LogLevel.Debug, "Sending quit command to MoodBar container process.");
            this.Send(new MoodBarCommand(MoodBarCommandType.Quit));
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
                Logger.Write(this, LogLevel.Warn, "Moodbar container process did not exit after {0}ms, terminating it.", TIMEOUT);
                this.Process.Kill();
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to terminate moodbar container process: {0}", e.Message);
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

        protected virtual void UpdateStatus(MoodBarStatus status)
        {
            Logger.Write(this, LogLevel.Debug, "Recieved status from MoodBar container process: {0}", Enum.GetName(typeof(MoodBarStatusType), status.Type));
            switch (status.Type)
            {
                case MoodBarStatusType.Complete:
                case MoodBarStatusType.Error:
                    Logger.Write(this, LogLevel.Debug, "Fetching final status and shutting down moodbar container process.");
                    this.Update();
                    this.Quit();
                    this.Process.StandardInput.Close();
                    this.Process.StandardOutput.Close();
                    break;
            }
        }

        protected virtual void UpdateItems(IEnumerable<MoodBarItem> moodBarItems)
        {
            foreach (var source in moodBarItems)
            {
                var destination = this.MoodBarItems.FirstOrDefault(moodBarItem => moodBarItem.Id == source.Id);
                if (source != null && destination != null)
                {
                    this.UpdateItem(source, destination);
                }
            }
        }

        protected virtual void UpdateItem(MoodBarItem source, MoodBarItem destination)
        {
            if (destination.Data == null)
            {
                destination.Data = source.Data;
                if (destination.Data != null)
                {
                    destination.Data.Update();
                }
            }
            else
            {
                for (var a = 0; a < source.Data.Data.GetLength(0); a++)
                {
                    for (var b = 0; b < source.Data.Data.GetLength(1); b++)
                    {
                        destination.Data.Data[a, b] = source.Data.Data[a, b];
                    }
                }
                destination.Data.Update();
            }
            destination.Errors = source.Errors;
            destination.Progress = source.Progress;
            destination.Status = source.Status;
        }

        public void Prune()
        {
            throw new NotImplementedException();
        }

        protected virtual void OnUpdated()
        {
            if (this.Updated == null)
            {
                return;
            }
            this.Updated(this, EventArgs.Empty);
        }

        public event EventHandler Updated;

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

        ~MoodBarProxy()
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
