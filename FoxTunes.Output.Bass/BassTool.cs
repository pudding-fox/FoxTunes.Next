using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace FoxTunes
{
    public abstract class BassTool : BaseComponent
    {
        protected BassTool()
        {
            this.CancellationToken = new CancellationToken();
        }

        public CancellationToken CancellationToken { get; private set; }

        public int Threads { get; protected set; }

        public Process Process
        {
            get
            {
                return Process.GetCurrentProcess();
            }
        }

        public override void InitializeComponent(ICore core)
        {
            Logger.Write(this, LogLevel.Debug, "Initializing BASS (NoSound).");
            BassUtils.OK(Bass.Init(Bass.NoSoundDevice));
            base.InitializeComponent(core);
        }

        public virtual void Update()
        {
            //Nothing to do.
        }

        public virtual void Cancel()
        {
            if (this.CancellationToken.IsCancellationRequested)
            {
                return;
            }
            Logger.Write(this, LogLevel.Warn, "Cancellation requested, shutting down.");
            this.CancellationToken.Cancel();
        }

        protected virtual bool CheckInput(string fileName)
        {
            if (!string.IsNullOrEmpty(Path.GetPathRoot(fileName)) && !File.Exists(fileName))
            {
                //TODO: Bad .Result
                if (!NetworkDrive.IsRemotePath(fileName) || !NetworkDrive.ConnectRemotePath(fileName).Result)
                {
                    throw new FileNotFoundException(string.Format("File not found: {0}", fileName), fileName);
                }
            }
            return true;
        }

        protected virtual bool CheckOutput(string fileName)
        {
            var directoryName = Path.GetDirectoryName(fileName);
            if (!string.IsNullOrEmpty(Path.GetPathRoot(directoryName)) && !Directory.Exists(directoryName))
            {
                //TODO: Bad .Result
                if (!NetworkDrive.IsRemotePath(directoryName) || !NetworkDrive.ConnectRemotePath(directoryName).Result)
                {
                    try
                    {
                        Directory.CreateDirectory(directoryName);
                    }
                    catch
                    {
                        throw new DirectoryNotFoundException(string.Format("Directory not found: {0}", directoryName));
                    }
                }
            }
            return true;
        }

        protected virtual IBassStream CreateStream(string fileName, BassFlags flags)
        {
            const int INTERVAL = 5000;
            var streamFactory = ComponentRegistry.Instance.GetComponent<IBassStreamFactory>();
            var playlistItem = new PlaylistItem()
            {
                FileName = fileName
            };
        retry:
            if (this.CancellationToken.IsCancellationRequested)
            {
                return BassStream.Empty;
            }
            var stream = streamFactory.CreateBasicStream(
                playlistItem,
                flags
            );
            if (stream.IsPending)
            {
                Logger.Write(this, LogLevel.Trace, "Failed to create decoder stream for file \"{0}\": Device is already in use.", fileName);
                Thread.Sleep(INTERVAL);
                goto retry;
            }
            if (stream.IsEmpty)
            {
                throw new InvalidOperationException(string.Format("Failed to create decoder stream for file \"{0}\": {1}", fileName, Enum.GetName(typeof(Errors), stream.Errors)));
            }
            return stream;
        }

        protected virtual bool Join(Thread thread)
        {
            const int INTERVAL = 5000;
            while (thread.IsAlive)
            {
                if (thread.Join(INTERVAL))
                {
                    break;
                }
                if (this.CancellationToken.IsCancellationRequested)
                {
                    if (thread.Join(INTERVAL))
                    {
                        break;
                    }
                    thread.Abort();
                    return false;
                }
            }
            return true;
        }

        protected virtual bool WaitForExit(Process process)
        {
            const int INTERVAL = 5000;
            while (!process.HasExited)
            {
                if (process.WaitForExit(INTERVAL))
                {
                    break;
                }
                if (this.CancellationToken.IsCancellationRequested)
                {
                    if (process.WaitForExit(INTERVAL))
                    {
                        break;
                    }
                    process.Kill();
                    return false;
                }
            }
            return true;
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
            Logger.Write(this, LogLevel.Debug, "Releasing BASS (NoSound).");
            Bass.Free();
        }

        ~BassTool()
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
