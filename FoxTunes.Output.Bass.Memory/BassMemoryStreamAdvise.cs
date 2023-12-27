using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Memory;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public class BassMemoryStreamAdvise : BassStreamAdvice
    {
        public BassMemoryStreamAdvise(string fileName) : base(fileName)
        {

        }

        public override bool Wrap(IBassStreamProvider provider, int channelHandle, IEnumerable<IBassStreamAdvice> advice, BassFlags flags, out IBassStream stream)
        {
            if (BassUtils.GetChannelDsdRaw(channelHandle))
            {
                //Only PCM.
                stream = null;
                return false;
            }
            Logger.Write(this, LogLevel.Debug, "Creating memory stream for channel: {0}", channelHandle);
            var memoryChannelHandle = BassMemory.CreateStream(channelHandle, 0, 0, flags);
            if (memoryChannelHandle == 0)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create memory stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
                stream = null;
                return false;
            }
            Logger.Write(this, LogLevel.Debug, "Created memory stream: {0}", channelHandle);
            stream = new BassStream(
                provider,
                memoryChannelHandle,
                Bass.ChannelGetLength(channelHandle, PositionFlags.Bytes),
                advice,
                flags
            );
            provider.FreeStream(channelHandle);
            return true;
        }
    }
}
