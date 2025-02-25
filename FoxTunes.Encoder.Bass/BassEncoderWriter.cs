using FoxTunes.Interfaces;
using System.IO;

namespace FoxTunes
{
    public abstract class BassEncoderWriter : BaseComponent, IBassEncoderWriter
    {
        public BassEncoderWriter(Stream stream)
        {
            this.Stream = stream;
        }

        public Stream Stream { get; private set; }

        public virtual void Write(byte[] data, int length)
        {
            this.Stream.Write(data, 0, length);
        }

        public virtual void Close()
        {
            this.Stream.Close();
        }
    }
}
