using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FoxTunes.Mpeg4
{
    public class XtraBoxParser
    {
        public XtraBoxParser(byte[] data)
        {
            this.OnParse(data);
        }

        public IEnumerable<XtraTag> Tags { get; private set; }

        protected virtual void OnParse(byte[] data)
        {
            var tags = new List<XtraTag>();
            using (var stream = new MemoryStream(data))
            {
                using (var reader = new BinaryReader(stream))
                {
                    while (stream.Position < stream.Length)
                    {
                        tags.Add(this.OnParse(reader));
                    }
                }
                this.Tags = tags.ToArray();
            }
        }

        protected virtual XtraTag OnParse(BinaryReader reader)
        {
            var parts = new List<XtraTagPart>();
            var frameLength = ReadInt32(reader);
            var nameLength = ReadInt32(reader);
            var name = ReadString(reader, nameLength);
            var count = ReadInt32(reader);
            var total = 4 + 4 + nameLength + 4;
            for (var position = 0; position < count; position++)
            {
                var elementLength = ReadInt32(reader);
                var flag = ReadInt16(reader);
                var contentLength = elementLength - (4 + 2);
                var type = default(XtraTagType);
                if (flag == XtraBox.UNICODE)
                {
                    type = XtraTagType.Unicode;
                }
                else if (flag == XtraBox.UINT64 && contentLength == 8)
                {
                    type = XtraTagType.UInt64;
                }
                else if (flag == XtraBox.DATE && contentLength == 8)
                {
                    type = XtraTagType.Date;
                }
                else if (flag == XtraBox.GUID && contentLength == 16)
                {
                    type = XtraTagType.Guid;
                }
                else if (flag == XtraBox.VARIANT && contentLength > 4)
                {
                    type = XtraTagType.Variant;
                }
                else
                {
                    type = XtraTagType.Unknown;
                }
                var content = new byte[contentLength];
                reader.Read(content, 0, content.Length);

                parts.Add(new XtraTagPart(type, content));

                total += elementLength;
            }

            if (total != frameLength)
            {
                throw new InvalidOperationException(string.Format("Xtra frame is invalid: Expected {0} bytes but found {1}.", frameLength, total));
            }

            return new XtraTag(name, parts.ToArray());
        }

        private static short ReadInt16(BinaryReader reader)
        {
            var buffer = new byte[2];
            reader.Read(buffer, 0, buffer.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            return BitConverter.ToInt16(buffer, 0);
        }

        private static int ReadInt32(BinaryReader reader)
        {
            var buffer = new byte[4];
            reader.Read(buffer, 0, buffer.Length);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            return BitConverter.ToInt32(buffer, 0);
        }

        private static string ReadString(BinaryReader reader, int length)
        {
            var buffer = new byte[length];
            reader.Read(buffer, 0, buffer.Length);
            if (buffer[0] == 0)
            {
                return string.Empty;
            }
            return Encoding.UTF8.GetString(buffer);
        }
    }
}
