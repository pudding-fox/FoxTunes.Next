namespace FoxTunes
{
    public static class MetaDataInfo
    {
        public static string BitRateDescription(int rate)
        {
            return string.Format("{0} kb/s", rate);
        }

        public static string SampleRateDescription(int rate)
        {
            var a = default(float);
            var suffix = default(string);
            if (rate < 1000000)
            {
                a = rate / 1000f;
                suffix = "k";
            }
            else
            {
                a = rate / 1000000f;
                suffix = "m";
            }
            return string.Format("{0:0.#}{1}Hz", a, suffix);
        }

        public static string ChannelDescription(int channels)
        {
            switch (channels)
            {
                case 1:
                    return "mono";
                case 2:
                    return "stereo";
                case 4:
                    return "quad";
                case 6:
                    return "5.1";
                case 8:
                    return "7.1";
            }
            return string.Format("{0} channels", channels);
        }

        public static string SampleDescription(int depth)
        {
            switch (depth)
            {
                case 0:
                    //It's as good a guess as any.
                    depth = 16;
                    break;
                case 1:
                    return "DSD";
            }
            return string.Format("{0} bit", depth);
        }
    }
}
