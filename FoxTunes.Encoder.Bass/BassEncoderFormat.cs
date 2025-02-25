using System.Linq;

namespace FoxTunes
{
    public class BassEncoderFormat : IBassEncoderFormat
    {
        public BassEncoderFormat(BassEncoderBinaryFormat binaryFormat, BassEncoderBinaryEndian binaryEndian, int depth, params int[] sampleRates)
        {
            this.BinaryFormat = binaryFormat;
            this.BinaryEndian = binaryEndian;
            this.Depth = depth;
            if (sampleRates != null && sampleRates.Any())
            {
                this.SampleRates = sampleRates;
            }
            else
            {
                this.SampleRates = OutputRate.PCM;
            }
        }

        public BassEncoderBinaryFormat BinaryFormat { get; private set; }

        public BassEncoderBinaryEndian BinaryEndian { get; private set; }

        public bool AutoDepth
        {
            get
            {
                return this.Depth == 0;
            }
        }

        public int Depth { get; private set; }

        public int[] SampleRates { get; private set; }
    }
}
