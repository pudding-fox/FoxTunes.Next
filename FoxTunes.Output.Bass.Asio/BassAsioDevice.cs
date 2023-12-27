using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Asio;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public static class BassAsioDevice
    {
        public const int MASTER_CHANNEL = -1;

        public const int PRIMARY_CHANNEL = 0;

        private static ILogger Logger
        {
            get
            {
                return LogManager.Logger;
            }
        }

        static BassAsioDevice()
        {
            //Perhaps we shouldn't Reset each time the output is started.
            //But what if the system config changes and the current device id isn't what we think it is?
            //var configuration = ComponentRegistry.Instance.GetComponent<IConfiguration>();
            //configuration.GetElement<SelectionConfigurationElement>(
            //    BassOutputConfiguration.SECTION,
            //    BassAsioStreamOutputConfiguration.ELEMENT_ASIO_DEVICE
            //).ConnectValue(value => Reset());
            //configuration.GetElement<CommandConfigurationElement>(
            //    BassOutputConfiguration.SECTION,
            //    BassAsioStreamOutputConfiguration.ELEMENT_REFRESH
            //).Invoked += (sender, e) => Reset();
        }

        public static bool IsInitialized { get; private set; }

        public static void Init(int device, int rate, int channels, BassFlags flags)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Device is already initialized.");
            }

            IsInitialized = true;

            Logger.Write(typeof(BassAsioDevice), LogLevel.Debug, "Initializing BASS ASIO.");

            try
            {
                BassAsioUtils.OK(BassAsio.Init(device, AsioInitFlags.Thread));
                BassAsioUtils.OK(BassAsioHandler.Init());
                BassAsioUtils.OK(BassAsioHandler.ChannelEnable(false, PRIMARY_CHANNEL));
                for (var channel = 1; channel < channels; channel++)
                {
                    Logger.Write(typeof(BassAsioDevice), LogLevel.Debug, "Joining channel: {0} => {1}", channel, PRIMARY_CHANNEL);
                    BassAsioUtils.OK(BassAsio.ChannelJoin(false, channel, PRIMARY_CHANNEL));
                }

                if (flags.HasFlag(BassFlags.DSDRaw))
                {
                    InitDSD(device, rate, channels, flags);
                }
                else
                {
                    InitPCM(device, rate, channels, flags);
                }

                BassAsio.Rate = rate;

                Logger.Write(typeof(BassAsioDevice), LogLevel.Debug, "Initialized BASS ASIO.");
            }
            catch
            {
                Free();
                throw;
            }
        }

        private static void InitPCM(int device, int rate, int channels, BassFlags flags)
        {
            BassAsioUtils.OK(BassAsio.SetDSD(false));
            BassAsioUtils.OK(BassAsio.ChannelSetRate(false, BassAsioDevice.PRIMARY_CHANNEL, rate));
            var format = default(AsioSampleFormat);
            if (flags.HasFlag(BassFlags.Float))
            {
                format = AsioSampleFormat.Float;
            }
            else
            {
                format = AsioSampleFormat.Bit16;
            }
            BassAsioUtils.OK(BassAsio.ChannelSetFormat(false, BassAsioDevice.PRIMARY_CHANNEL, format));
        }

        private static void InitDSD(int device, int rate, int channels, BassFlags flags)
        {
            BassAsioUtils.OK(BassAsio.SetDSD(true));
            //It looks like BASS DSD always outputs 8 bit/MSB data so we don't need to determine the format.
            //var format = default(AsioSampleFormat);
            //switch (this.Depth)
            //{
            //    case BassAttribute.DSDFormat_LSB:
            //        format = AsioSampleFormat.DSD_LSB;
            //        break;
            //    case BassAttribute.DSDFormat_None:
            //    case BassAttribute.DSDFormat_MSB:
            //        format = AsioSampleFormat.DSD_MSB;
            //        break;
            //    default:
            //        throw new NotImplementedException();
            //}
            BassAsioUtils.OK(BassAsio.ChannelSetFormat(false, BassAsioDevice.PRIMARY_CHANNEL, AsioSampleFormat.DSD_MSB));
        }

        public static void Detect(int device)
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("Device is already initialized.");
            }

            IsInitialized = true;

            Logger.Write(typeof(BassAsioDevice), LogLevel.Debug, "Detecting ASIO device.");

            try
            {
                BassAsioUtils.OK(BassAsio.Init(device, AsioInitFlags.Thread));
                BassAsioUtils.OK(BassAsioHandler.Init());
                var info = default(AsioChannelInfo);
                BassAsioUtils.OK(BassAsio.ChannelGetInfo(false, PRIMARY_CHANNEL, out info));
                Info = new BassAsioDeviceInfo(
                    BassAsio.CurrentDevice,
                    Convert.ToInt32(BassAsio.Rate),
                    BassAsio.Info.Inputs,
                    BassAsio.Info.Outputs,
                    GetSupportedRates(),
                    info.Format
                );
                Logger.Write(typeof(BassAsioDevice), LogLevel.Debug, "Detected ASIO device: {0} => Inputs => {1}, Outputs = {2}, Rate = {3}, Format = {4}", BassAsio.CurrentDevice, Info.Inputs, Info.Outputs, Info.Rate, Enum.GetName(typeof(AsioSampleFormat), Info.Format));
                Logger.Write(typeof(BassAsioDevice), LogLevel.Debug, "Detected ASIO device: {0} => Rates => {1}", BassAsio.CurrentDevice, string.Join(", ", Info.SupportedRates));
            }
            finally
            {
                Free();
            }
        }

        public static void Reset()
        {
            Info = null;
        }

        private static IEnumerable<int> GetSupportedRates()
        {
            //TODO: I'm not sure if we should be setting DSD mode before querying DSD rates.
            return Enumerable.Concat(
                OutputRate.PCM,
                OutputRate.DSD
            ).Where(rate => BassAsio.CheckRate(rate)).ToArray();
        }

        public static void Free()
        {
            if (!IsInitialized)
            {
                return;
            }

            if (Info != null)
            {
                var flags =
                    AsioChannelResetFlags.Enable |
                    AsioChannelResetFlags.Join |
                    AsioChannelResetFlags.Format |
                    AsioChannelResetFlags.Rate;
                Logger.Write(typeof(BassAsioDevice), LogLevel.Debug, "Resetting BASS ASIO channel attributes.");
                for (var channel = 0; channel < Info.Outputs; channel++)
                {
                    BassAsio.ChannelReset(false, channel, flags);
                }
            }

            Logger.Write(typeof(BassAsioDevice), LogLevel.Debug, "Releasing BASS ASIO.");
            BassAsio.Free();
            BassAsioHandler.Free();
            IsInitialized = false;
        }

        public static bool CanControlVolume
        {
            get
            {
                var volume = BassAsio.ChannelGetVolume(false, MASTER_CHANNEL);
                if (volume == -1)
                {
                    //Device (or driver) does not support this.
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public static float Volume
        {
            get
            {
                var volume = BassAsio.ChannelGetVolume(false, MASTER_CHANNEL);
                if (volume == -1)
                {
                    //100% I suppose.
                    return 1;
                }
                else
                {
                    return Convert.ToSingle(volume);
                }
            }
            set
            {
                if (!BassAsio.ChannelSetVolume(false, MASTER_CHANNEL, value))
                {
                    Logger.Write(typeof(BassAsioDevice), LogLevel.Warn, "Cannot set volume, the device or driver probably doesn't support it.");
                }
            }
        }

        public static BassAsioDeviceInfo Info { get; private set; }

        public class BassAsioDeviceInfo
        {
            public BassAsioDeviceInfo(int device, int rate, int inputs, int outputs, IEnumerable<int> supportedRates, AsioSampleFormat format)
            {
                this.Device = device;
                this.Rate = rate;
                this.Inputs = inputs;
                this.Outputs = outputs;
                this.SupportedRates = supportedRates;
                this.Format = format;
            }

            public int Device { get; private set; }

            public int Rate { get; private set; }

            public int Inputs { get; private set; }

            public int Outputs { get; private set; }

            public IEnumerable<int> SupportedRates { get; private set; }

            public AsioSampleFormat Format { get; private set; }

            public bool ControlPanel()
            {
                return BassAsio.ControlPanel();
            }

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
