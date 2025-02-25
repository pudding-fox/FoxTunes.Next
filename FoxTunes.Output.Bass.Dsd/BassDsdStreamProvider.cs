using FoxDb;
using FoxTunes;
using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Dsd;
using ManagedBass.Memory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class BassDsdStreamProvider : BassStreamProvider
    {
        public BassDsdBehaviour Behaviour { get; private set; }

        public IBassStreamPipelineFactory BassStreamPipelineFactory { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Behaviour = ComponentRegistry.Instance.GetComponent<BassDsdBehaviour>();
            this.BassStreamPipelineFactory = ComponentRegistry.Instance.GetComponent<IBassStreamPipelineFactory>();
            base.InitializeComponent(core);
        }

        public override bool CanCreateStream(PlaylistItem playlistItem)
        {
            if (!BassDsdBehaviour.EXTENSIONS.Contains(playlistItem.FileName.GetExtension(), StringComparer.OrdinalIgnoreCase))
            {
                return false;
            }
            return true;
        }

        public override IBassStream CreateInteractiveStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, bool immidiate, BassFlags flags)
        {
            var channelHandle = default(int);
            if (this.IsDsdRawSupported())
            {
                channelHandle = this.CreateDsdRawStream(playlistItem, advice, flags);
                if (channelHandle == 0)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to create DSD RAW stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
                }
                return this.CreateInteractiveStream(channelHandle, advice, flags | BassFlags.DSDRaw);
            }
            else
            {
                channelHandle = this.CreateDsdStream(playlistItem, advice, flags);
                if (channelHandle == 0)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to create DSD stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
                }
                return this.CreateInteractiveStream(channelHandle, advice, flags);
            }
        }

        protected virtual int CreateDsdStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, BassFlags flags)
        {
            var fileName = this.GetFileName(playlistItem, advice);
            var channelHandle = default(int);
            if (this.Behaviour != null && this.Behaviour.Memory)
            {
                Logger.Write(this, LogLevel.Debug, "Creating memory stream for file: {0}", fileName);
                channelHandle = BassMemory.Dsd.CreateStream(fileName, 0, 0, flags);
                if (channelHandle != 0)
                {
                    Logger.Write(this, LogLevel.Debug, "Created memory stream: {0}", channelHandle);
                }
                else
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to create memory stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
                }
            }
            if (channelHandle == 0)
            {
                channelHandle = BassDsd.CreateStream(fileName, 0, 0, flags);
            }
            return channelHandle;
        }

        protected virtual int CreateDsdRawStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, BassFlags flags)
        {
            var fileName = this.GetFileName(playlistItem, advice);
            var channelHandle = default(int);
            if (this.Behaviour != null && this.Behaviour.Memory)
            {
                Logger.Write(this, LogLevel.Debug, "Creating memory stream for file: {0}", fileName);
                channelHandle = BassMemory.Dsd.CreateStream(fileName, 0, 0, flags | BassFlags.DSDRaw);
                if (channelHandle != 0)
                {
                    Logger.Write(this, LogLevel.Debug, "Created memory stream: {0}", channelHandle);
                }
                else
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to create memory stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
                }
            }
            if (channelHandle == 0)
            {
                channelHandle = BassDsd.CreateStream(fileName, 0, 0, flags | BassFlags.DSDRaw);
            }
            if (channelHandle == 0)
            {
                return channelHandle;
            }
            if (!this.IsFormatSupported(channelHandle))
            {
                this.FreeStream(channelHandle);
                channelHandle = 0;
            }
            return channelHandle;
        }

        protected virtual bool IsDsdRawSupported()
        {
            if (this.BassStreamPipelineFactory == null)
            {
                return false;
            }
            var query = this.BassStreamPipelineFactory.QueryPipeline();
            if ((query.OutputCapabilities & BassCapability.DSD_RAW) != BassCapability.DSD_RAW)
            {
                return false;
            }
            return true;
        }

        protected virtual bool IsFormatSupported(int channelHandle)
        {
            if (this.BassStreamPipelineFactory == null)
            {
                return false;
            }
            var query = this.BassStreamPipelineFactory.QueryPipeline();
            var channels = BassUtils.GetChannelCount(channelHandle);
            var rate = BassUtils.GetChannelDsdRate(channelHandle);
            if (query.OutputChannels < channels || !query.OutputRates.Contains(rate))
            {
                Logger.Write(this, LogLevel.Warn, "DSD format {0}:{1} is unsupported, the stream will be unloaded. This warning is expensive, please don't attempt to play unsupported DSD.", rate, channels);
                return false;
            }
            return true;
        }
    }
}
