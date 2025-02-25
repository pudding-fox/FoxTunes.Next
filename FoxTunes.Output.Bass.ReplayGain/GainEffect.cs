using System;

namespace FoxTunes
{
    public class ReplayGainEffect : VolumeEffect
    {
        public ReplayGainEffect(int channelHandle, float replayGain, ReplayGainMode mode) : base(channelHandle)
        {
            this.ReplayGain = replayGain;
            this.Mode = mode;
            this.Channel = 0; //Master channel.
            this.Volume = GetVolume(replayGain);
        }

        public float ReplayGain { get; private set; }

        public ReplayGainMode Mode { get; private set; }

        public static float GetVolume(float replayGain)
        {
            return Convert.ToSingle(Math.Pow(10, replayGain / 20));
        }
    }
}
