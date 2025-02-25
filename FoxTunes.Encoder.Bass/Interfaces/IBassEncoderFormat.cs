using System;

namespace FoxTunes
{
    public interface IBassEncoderFormat
    {
        BassEncoderBinaryFormat BinaryFormat { get; }

        BassEncoderBinaryEndian BinaryEndian { get; }

        bool AutoDepth { get; }

        int Depth { get; }

        int[] SampleRates { get; }
    }

    [Flags]
    public enum BassEncoderBinaryFormat : byte
    {
        None = 0,
        SignedInteger = 1,
        UnsignedInteger = 2,
        FloatingPoint = 4,
        All = SignedInteger | UnsignedInteger | FloatingPoint
    }

    public enum BassEncoderBinaryEndian : byte
    {
        None,
        Little,
        Big
    }
}
