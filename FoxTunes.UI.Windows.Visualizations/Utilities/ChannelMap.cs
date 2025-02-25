using FoxTunes.Interfaces;

namespace FoxTunes
{
    public static class ChannelMap
    {
        public static string GetChannelName(OutputChannel channel)
        {
            switch (channel)
            {
                case OutputChannel.Left:
                    return Strings.Speakers_Left;
                case OutputChannel.Right:
                    return Strings.Speakers_Right;
                case OutputChannel.FrontLeft:
                    return Strings.Speakers_FrontLeft;
                case OutputChannel.FrontRight:
                    return Strings.Speakers_FrontRight;
                case OutputChannel.RearLeft:
                    return Strings.Speakers_RearLeft;
                case OutputChannel.RearRight:
                    return Strings.Speakers_RearRight;
                case OutputChannel.Center:
                    return Strings.Speakers_Center;
                case OutputChannel.LFE:
                    return Strings.Speakers_LFE;
            }
            return Strings.Speakers_Unknown;
        }
    }
}
