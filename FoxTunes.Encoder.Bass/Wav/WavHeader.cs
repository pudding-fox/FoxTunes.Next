using System;
using System.IO;
using System.Text;

namespace FoxTunes
{
    public static class WavHeader
    {
        public static readonly byte[] WAV_TAG_RIFF = Encoding.ASCII.GetBytes("RIFF");

        public static readonly byte[] WAV_TAG_WAVE = Encoding.ASCII.GetBytes("WAVE");

        public static readonly string WAV_CHUNK_FMT = "fmt ";

        public static readonly string WAV_CHUNK_DATA = "data";

        public const int WAV_FORMAT_PCM = 0x1;

        public static bool Write(Stream stream, WavInfo info)
        {
            var writer = new BinaryWriter(stream);
            writer.Write(WAV_TAG_RIFF);
            writer.Write(LEWord32(info.DataSize + 36));
            writer.Write(WAV_TAG_WAVE);
            writer.Write(Encoding.ASCII.GetBytes(WAV_CHUNK_FMT));
            writer.Write(LEWord32(16));
            writer.Write(LEWord16(info.Format));
            writer.Write(LEWord16(info.ChannelCount));
            writer.Write(LEWord32(info.SampleRate));
            writer.Write(LEWord32(info.ByteRate));
            writer.Write(LEWord16(info.BlockAlign));
            writer.Write(LEWord16(info.BitsPerSample));
            writer.Write(Encoding.ASCII.GetBytes(WAV_CHUNK_DATA));
            writer.Write(LEWord32(info.DataSize));
            return true;
        }

        public static byte[] LEWord32(int value)
        {
            var buffer = BitConverter.GetBytes(value);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            return buffer;
        }

        public static byte[] LEWord16(int value)
        {
            var buffer = BitConverter.GetBytes(Convert.ToInt16(value));
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(buffer);
            }
            return buffer;
        }

        public struct WavInfo
        {
            public int Format;

            public int ChannelCount;

            public int SampleRate;

            public int ByteRate;

            public int BlockAlign;

            public int BitsPerSample;

            public int DataSize;
        }
    }
}
