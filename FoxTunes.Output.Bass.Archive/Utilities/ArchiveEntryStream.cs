using ManagedBass.ZipStream;
using System;
using System.IO;

namespace FoxTunes
{
    public class ArchiveEntryStream : Stream
    {
        private IntPtr Entry;

        public ArchiveEntryStream(IntPtr entry)
        {
            this.Entry = entry;
        }

        public override bool CanRead
        {
            get
            {
                return true;
            }
        }

        public override bool CanSeek
        {
            get
            {
                return true;
            }
        }

        public override bool CanWrite
        {
            get
            {
                return false;
            }
        }

        public override long Length
        {
            get
            {
                return ArchiveEntry.GetEntryLength(this.Entry);
            }
        }

        public override long Position
        {
            get
            {
                return ArchiveEntry.GetEntryPosition(this.Entry);
            }
            set
            {
                this.Seek(value, SeekOrigin.Begin);
            }
        }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (offset != 0)
            {
                //I don't really understand why but taglib occasionally calls this routing with a non zero offset.
                //Repeating the same operation does not yield the same behaviour.
                //bass_zipstream doesn't support offsets so here we are.
                //This is slow but we don't end up here often.
                //Weh.
                var temp = new byte[count];
                var result = ArchiveEntry.ReadEntry(this.Entry, temp, count);
                Array.Copy(temp, 0, buffer, offset, result);
                return result;
            }
            return ArchiveEntry.ReadEntry(this.Entry, buffer, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    break;
                case SeekOrigin.Current:
                    offset = this.Position + offset;
                    break;
                case SeekOrigin.End:
                    offset = this.Length + offset;
                    break;
            }
            if (!ArchiveEntry.SeekEntry(this.Entry, offset))
            {
                return 0;
            }
            return offset;
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }
}
