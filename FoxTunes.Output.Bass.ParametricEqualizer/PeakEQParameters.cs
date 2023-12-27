using ManagedBass;
using System;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class PeakEQParameters : IEffectParameter
    {
        public int lBand;

        public float fBandwidth;

        public float fQ;

        public float fCenter;

        public float fGain;

        public int lChannel;

        public EffectType FXType
        {
            get
            {
                return EffectType.PeakEQ;
            }
        }
    }
}
