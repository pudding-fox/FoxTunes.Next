using FoxTunes.Interfaces;
using System.Collections.Generic;

namespace FoxTunes
{
    public static class BassChannelMap
    {
        public static readonly IDictionary<int, OutputChannel> MONO = new Dictionary<int, OutputChannel>()
        {
            { 0, OutputChannel.Center }
        };

        public static readonly IDictionary<int, OutputChannel> STEREO = new Dictionary<int, OutputChannel>()
        {
            { 0, OutputChannel.Left },
            { 1, OutputChannel.Right }
        };

        public static readonly IDictionary<int, OutputChannel> SURROUND_3_0 = new Dictionary<int, OutputChannel>()
        {
            { 0, OutputChannel.Left },
            { 1, OutputChannel.Right },
            { 2, OutputChannel.Center }
        };

        public static readonly IDictionary<int, OutputChannel> QUAD = new Dictionary<int, OutputChannel>()
        {
            { 0, OutputChannel.FrontLeft },
            { 1, OutputChannel.FrontRight },
            { 2, OutputChannel.RearLeft },
            { 3, OutputChannel.RearRight }
        };

        public static readonly IDictionary<int, OutputChannel> SURROUND_5_1 = new Dictionary<int, OutputChannel>()
        {
            { 0, OutputChannel.FrontLeft },
            { 1, OutputChannel.FrontRight },
            { 2, OutputChannel.Center },
            { 3, OutputChannel.LFE },
            { 4, OutputChannel.RearLeft },
            { 5, OutputChannel.RearRight }
        };

        public static readonly IDictionary<int, OutputChannel> SURROUND_7_1 = new Dictionary<int, OutputChannel>()
        {
            { 0, OutputChannel.FrontLeft },
            { 1, OutputChannel.FrontRight },
            { 2, OutputChannel.Center },
            { 3, OutputChannel.LFE },
            { 4, OutputChannel.RearLeft },
            { 5, OutputChannel.RearRight },
            { 6, OutputChannel.Left },
            { 7, OutputChannel.Right },
        };

        public static IDictionary<int, OutputChannel> GetChannelMap(int channels)
        {
            switch (channels)
            {
                case 1:
                    return MONO;
                case 2:
                    return STEREO;
                case 4:
                    return QUAD;
                case 6:
                    return SURROUND_5_1;
                case 8:
                    return SURROUND_7_1;
            }
            var result = new Dictionary<int, OutputChannel>();
            for (var a = 0; a < channels; a++)
            {
                result.Add(a, OutputChannel.None);
            }
            return result;
        }
    }
}
