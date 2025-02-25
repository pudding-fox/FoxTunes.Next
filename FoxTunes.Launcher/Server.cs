using FoxTunes.Interfaces;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FoxTunes
{
    public class Server : IDisposable
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        public static readonly string Id = typeof(Server).Assembly.FullName;

        public Server()
        {
            var success = default(bool);
            this.Mutex = new Mutex(true, Id, out success);
            if (success)
            {
                Task.Factory.StartNew(() => this.OnStart());
            }
            else
            {
                this.Dispose();
            }
        }

        public Mutex Mutex { get; private set; }

        protected virtual async Task OnStart()
        {
            using (var server = new NamedPipeServerStream(Id))
            {
                using (var reader = new StreamReader(server))
                {
                    while (!this.IsDisposed)
                    {
#if NET40
                        server.WaitForConnection();
#else
                        await server.WaitForConnectionAsync().ConfigureAwait(false);
#endif
                        await this.OnConnection(server, reader).ConfigureAwait(false);
                    }
                }
            }
        }

        protected virtual async Task OnConnection(NamedPipeServerStream server, StreamReader reader)
        {
            var builder = new StringBuilder();
            while (server.IsConnected)
            {
                builder.AppendLine(await reader.ReadLineAsync().ConfigureAwait(false));
            }
            this.OnMessage(builder.ToString());
            server.Disconnect();
        }

        protected virtual void OnMessage(string message)
        {
            if (this.Message == null)
            {
                return;
            }
            this.Message(this, new ListenerEventArgs(message));
        }

        public event ListenerEventHandler Message;

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
            this.Mutex.Dispose();
        }

        ~Server()
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
    }

    public delegate void ListenerEventHandler(object sender, ListenerEventArgs e);

    public class ListenerEventArgs : EventArgs
    {
        public ListenerEventArgs(string message)
        {
            this.Message = message;
        }

        public string Message { get; private set; }
    }
}
