using System.Linq;

namespace FoxTunes
{
    public static class OutputRate
    {
        public const int PCM_32000 = 32000;
        public const int PCM_44100 = 44100;
        public const int PCM_48000 = 48000;
        public const int PCM_88200 = 88200;
        public const int PCM_96000 = 96000;
        public const int PCM_176400 = 176400;
        public const int PCM_192000 = 192000;
        public const int PCM_352800 = 352800;
        public const int PCM_384000 = 384000;

        public static int[] PCM = new[]
        {
             PCM_44100,
             PCM_48000,
             PCM_88200,
             PCM_96000,
             PCM_176400,
             PCM_192000,
             PCM_352800,
             PCM_384000
        };

        public const int DSD_2822400 = 2822400;
        public const int DSD_3072000 = 3072000;
        public const int DSD_5644800 = 5644800;
        public const int DSD_6144000 = 6144000;
        public const int DSD_11289600 = 11289600;
        public const int DSD_12288000 = 12288000;
        public const int DSD_22579200 = 22579200;
        public const int DSD_24576000 = 24576000;

        public static int[] DSD64 = new[]
        {
            DSD_2822400,
            DSD_3072000
        };

        public static int[] DSD128 = new[]
        {
            DSD_5644800,
            DSD_6144000
        };

        public static int[] DSD256 = new[]
        {
            DSD_11289600,
            DSD_12288000
        };

        public static int[] DSD512 = new[]
        {
            DSD_22579200,
            DSD_24576000
        };

        public static int[] DSD = new[]
        {
            DSD_2822400,
            DSD_3072000,
            DSD_5644800,
            DSD_6144000,
            DSD_11289600,
            DSD_12288000,
            DSD_22579200,
            DSD_24576000
        };

        public static int[] GetRates(int current, int min, int max)
        {
            if (min == 0 || max == 0)
            {
                return new[] { current };
            }
            return PCM
                .Concat(DSD)
                .Where(rate => rate >= min && rate <= max)
                .ToArray();
        }
    }
}
