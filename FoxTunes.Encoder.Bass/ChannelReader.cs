using FoxTunes.Interfaces;
using ManagedBass;

namespace FoxTunes
{
    public class ChannelReader
    {
        protected static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        const int BUFFER_SIZE = 102400;

        const int BASS_ERROR_UNKNOWN = -1;

        const int BASS_STREAMPROC_END = -2147483648;

        public ChannelReader(EncoderItem encoderItem, IBassStream stream)
        {
            this.EncoderItem = encoderItem;
            this.Stream = stream;
        }

        public EncoderItem EncoderItem { get; private set; }

        public IBassStream Stream { get; private set; }

        public void CopyTo(ProcessWriter writer, CancellationToken cancellationToken)
        {
            try
            {
                this.CopyTo(writer.Write, cancellationToken);
                Logger.Write(this.GetType(), LogLevel.Debug, "Finished reading data from channel {0}, closing process input.", this.Stream.ChannelHandle);
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
                Logger.Write(this.GetType(), LogLevel.Debug, "Finished reading data from channel {0}, closing writer input.", this.Stream.ChannelHandle);
            }
            finally
            {
                writer.Close();
            }
        }

        public void CopyTo(Writer writer, CancellationToken cancellationToken)
        {
            Logger.Write(this.GetType(), LogLevel.Debug, "Begin reading data from channel {0} with {1} byte buffer.", this.Stream.ChannelHandle, BUFFER_SIZE);
            var buffer = new byte[BUFFER_SIZE];
            while (Bass.ChannelIsActive(this.Stream.ChannelHandle) != PlaybackState.Stopped)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                var length = Bass.ChannelGetData(this.Stream.ChannelHandle, buffer, BUFFER_SIZE);
                if (length == 0)
                {
                    continue;
                }
                switch (length)
                {
                    case BASS_STREAMPROC_END:
                    case BASS_ERROR_UNKNOWN:
                        return;
                }
                writer(buffer, length);
                this.Update();
            }
        }

        protected virtual void Update()
        {
            this.EncoderItem.Progress = (int)((this.Stream.Position / (double)this.Stream.Length) * EncoderItem.PROGRESS_COMPLETE);
        }

        public delegate void Writer(byte[] data, int length);
    }
}
