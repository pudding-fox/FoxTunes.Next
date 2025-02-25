using FoxTunes.Interfaces;
using ManagedBass;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public static class BassDirectSoundDevice
    {
        public static bool IsInitialized { get; private set; }

        public static bool IsDefaultDevice
        {
            get
            {
                return Info != null && Info.IsDefault;
            }
        }

        public static void Init(int device)
        {
            IsInitialized = true;
            Info = new BassDirectSoundDeviceInfo(
                Bass.CurrentDevice,
                0,
                Bass.Info.SpeakerCount,
                OutputRate.GetRates(Bass.Info.SampleRate, Bass.Info.MinSampleRate, Bass.Info.MaxSampleRate),
                device == Bass.DefaultDevice
            );
            LogManager.Logger.Write(typeof(BassDirectSoundDevice), LogLevel.Debug, "Detected DS device: {0} => Inputs => {1}, Outputs = {2}, Rate = {3}", Bass.CurrentDevice, Info.Inputs, Info.Outputs, Info.Rate);
            LogManager.Logger.Write(typeof(BassDirectSoundDevice), LogLevel.Debug, "Detected DS device: {0} => Rates => {1}", Bass.CurrentDevice, string.Join(", ", Info.SupportedRates));
        }

        public static void Free()
        {
            if (!IsInitialized)
            {
                return;
            }
            IsInitialized = false;
        }

        public static BassDirectSoundDeviceInfo Info { get; private set; }

        public class BassDirectSoundDeviceInfo
        {
            public BassDirectSoundDeviceInfo(int device, int inputs, int outputs, IEnumerable<int> supportedRates, bool isDefault)
            {
                this.Device = device;
                this.Inputs = inputs;
                this.Outputs = outputs;
                this.SupportedRates = supportedRates;
                this.IsDefault = isDefault;
            }

            public int Device { get; private set; }

            public int Rate
            {
                get
                {
                    return Bass.Info.SampleRate;
                }
            }

            public int Inputs { get; private set; }

            public int Outputs { get; private set; }

            public IEnumerable<int> SupportedRates { get; private set; }

            public bool IsDefault { get; private set; }

            public int GetNearestRate(int rate)
            {
                //Find the closest supported rate.
                foreach (var supportedRate in this.SupportedRates)
                {
                    if (supportedRate >= rate)
                    {
                        return supportedRate;
                    }
                }
                //Ah. The minimum supported rate is not enough.
                return this.SupportedRates.LastOrDefault();
            }
        }
    }
}
