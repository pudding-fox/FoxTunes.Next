using ManagedBass;
using System;
using System.Runtime.InteropServices;

namespace FoxTunes
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class VolumeParameters : IEffectParameter
    {
        public int lChannel;

        public float fVolume;

        public EffectType FXType
        {
            get
            {
                return EffectType.Volume;
            }
        }
    }
}
