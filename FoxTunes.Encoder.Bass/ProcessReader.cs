using FoxTunes.Interfaces;
using System.Diagnostics;

namespace FoxTunes
{
    public class ProcessReader
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        const int BUFFER_SIZE = 10240;

        public ProcessReader(Process process)
        {
            this.Process = process;
        }

        public Process Process { get; }

        public void CopyTo(ProcessWriter writer, CancellationToken cancellationToken)
        {
            try
            {
                this.CopyTo(writer.Write, cancellationToken);
                Logger.Write(this.GetType(), LogLevel.Debug, "Finished reading data from process {0}, closing process input.", this.Process.Id);
            }
            finally
            {
                writer.Close();
            }
        }

        public void CopyTo(IBassEncoderWriter writer, CancellationToken cancellationToken)
        {
            try
            {
                this.CopyTo(writer.Write, cancellationToken);
                Logger.Write(this.GetType(), LogLevel.Debug, "Finished reading data from process {0}, closing writer input.", this.Process.Id);
            }
            finally
            {
                writer.Close();
            }
        }

        public void CopyTo(Writer writer, CancellationToken cancellationToken)
        {
            Logger.Write(this.GetType(), LogLevel.Debug, "Begin reading data from process {0} with {1} byte buffer.", this.Process.Id, BUFFER_SIZE);
            var length = default(int);
            var buffer = new byte[BUFFER_SIZE];
            while (!this.Process.HasExited)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                while ((length = this.Process.StandardOutput.BaseStream.Read(buffer, 0, BUFFER_SIZE)) > 0)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                    writer(buffer, length);
                }
            }
        }

        public delegate void Writer(byte[] data, int length);
    }
}
