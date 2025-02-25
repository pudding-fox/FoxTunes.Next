using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace FoxTunes.Mpeg4
{
    public class XtraBoxFormatter
    {
        public XtraBoxFormatter(IEnumerable<XtraTag> tags)
        {
            this.OnFormat(tags);
        }

        public byte[] Data { get; private set; }

        protected virtual void OnFormat(IEnumerable<XtraTag> tags)
        {
            using (var stream = new MemoryStream())
            {
                using (var writer = new BinaryWriter(stream))
                {
                    foreach (var tag in tags)
                    {
                        this.OnFormat(writer, tag);
                    }
                    writer.Flush();
                    stream.Flush();
                    this.Data = stream.ToArray();
                }
            }
        }

        protected virtual void OnFormat(BinaryWriter writer, XtraTag tag)
        {
            var frameLength = default(int);
            var nameLength = default(int);
            var name = tag.Name;
            var count = tag.Parts.Length;
            var contentLength = default(int);

            contentLength = tag.Parts.Sum(part => 4 + 2 + part.Content.Length);
            nameLength = name.Length;
            frameLength = 4 + 4 + nameLength + 4 + contentLength;

            WriteInt32(writer, frameLength);
            WriteInt32(writer, nameLength);
            WriteString(writer, name);
            WriteInt32(writer, count);

            foreach (var part in tag.Parts)
            {
                var flag = default(short);
                var content = part.Content;
                var elementLength = 4 + 2 + content.Length;

                switch (part.Type)
                {
                    case XtraTagType.Unicode:
                        flag = XtraBox.UNICODE;
                        break;
                    case XtraTagType.UInt64:
                        flag = XtraBox.UINT64;
                        break;
                    case XtraTagType.Date:
                        flag = XtraBox.DATE;
                        break;
                    case XtraTagType.Guid:
                        flag = XtraBox.GUID;
                        break;
                    case XtraTagType.Variant:
                        flag = XtraBox.VARIANT;
                        break;
                    default:
                        throw new NotImplementedException();
                }

                WriteInt32(writer, elementLength);
                WriteInt16(writer, flag);
                writer.Write(content);
            }
        }

        private static void WriteInt16(BinaryWriter writer, short value)
        {
            var buffer = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            writer.Write(buffer);
        }

        private static void WriteInt32(BinaryWriter writer, int value)
        {
            var buffer = BitConverter.GetBytes(value);

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }

            writer.Write(buffer);
        }

        private static void WriteString(BinaryWriter writer, string value)
        {
            var buffer = Encoding.UTF8.GetBytes(value);

            writer.Write(buffer);
        }
    }
}
