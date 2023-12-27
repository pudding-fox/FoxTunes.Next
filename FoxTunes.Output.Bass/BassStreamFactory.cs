using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class BassStreamFactory : StandardComponent, IBassStreamFactory
    {
        public BassStreamFactory()
        {
            this.Advisors = new List<IBassStreamAdvisor>();
            this.Providers = new List<IBassStreamProvider>();
        }

        public List<IBassStreamAdvisor> Advisors { get; private set; }

        public List<IBassStreamProvider> Providers { get; private set; }

        public IBassOutput Output { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output as IBassOutput;
            this.Advisors.AddRange(ComponentRegistry.Instance.GetComponents<IBassStreamAdvisor>());
            this.Providers.AddRange(ComponentRegistry.Instance.GetComponents<IBassStreamProvider>());
            base.InitializeComponent(core);
        }

        public IEnumerable<IBassStreamAdvice> GetAdvice(IBassStreamProvider provider, PlaylistItem playlistItem, BassStreamUsageType type)
        {
            var advice = new List<IBassStreamAdvice>();
            foreach (var advisor in this.Advisors)
            {
                advisor.Advise(provider, playlistItem, advice, type);
            }
            return advice.ToArray();
        }

        public IEnumerable<IBassStreamProvider> GetProviders(PlaylistItem playlistItem)
        {
            return this.Providers.Where(
                provider => provider.CanCreateStream(playlistItem)
            ).ToArray();
        }

        public IBassStream CreateBasicStream(PlaylistItem playlistItem, BassFlags flags)
        {
            flags |= BassFlags.Decode;
            Logger.Write(this, LogLevel.Debug, "Attempting to create stream for file \"{0}\".", playlistItem.FileName);
            var providers = this.GetProviders(playlistItem);
            if (!providers.Any())
            {
                Logger.Write(this, LogLevel.Warn, "No provider was found for file \"{0}\".", playlistItem.FileName);
                return BassStream.Empty;
            }
            foreach (var provider in providers)
            {
                Logger.Write(this, LogLevel.Debug, "Using bass stream provider \"{0}\".", provider.GetType().Name);
                var advice = this.GetAdvice(provider, playlistItem, BassStreamUsageType.Basic).ToArray();
                var stream = provider.CreateBasicStream(playlistItem, advice, flags);
                if (stream.ChannelHandle != 0)
                {
                    Logger.Write(this, LogLevel.Debug, "Created stream from file {0}: {1}", playlistItem.FileName, stream.ChannelHandle);
                    return stream;
                }
                if (stream.Errors == Errors.Already && provider.Flags.HasFlag(BassStreamProviderFlags.Serial))
                {
                    Logger.Write(this, LogLevel.Debug, "Provider does not support multiple streams.");
                    return stream;
                }
                Logger.Write(this, LogLevel.Debug, "Failed to create stream from file {0}: {1}", playlistItem.FileName, Enum.GetName(typeof(Errors), stream.Errors));
            }
            Logger.Write(this, LogLevel.Warn, "All providers failed for file \"{0}\".", playlistItem.FileName);
            return BassStream.Empty;
        }

        public IBassStream CreateInteractiveStream(PlaylistItem playlistItem, bool immidiate, BassFlags flags)
        {
            flags |= BassFlags.Decode;
            Logger.Write(this, LogLevel.Debug, "Attempting to create stream for file \"{0}\".", playlistItem.FileName);
            var providers = this.GetProviders(playlistItem);
            if (!providers.Any())
            {
                Logger.Write(this, LogLevel.Warn, "No provider was found for file \"{0}\".", playlistItem.FileName);
                return BassStream.Empty;
            }
            foreach (var provider in providers)
            {
                Logger.Write(this, LogLevel.Debug, "Using bass stream provider \"{0}\".", provider.GetType().Name);
                var advice = this.GetAdvice(provider, playlistItem, BassStreamUsageType.Interactive).ToArray();
                var stream = provider.CreateInteractiveStream(playlistItem, advice, immidiate, flags);
                if (!stream.IsEmpty)
                {
                    Logger.Write(this, LogLevel.Debug, "Created stream from file {0}: {1}", playlistItem.FileName, stream.ChannelHandle);
                    return stream;
                }
                else if (stream.IsPending)
                {
                    Logger.Write(this, LogLevel.Debug, "Failed to create stream from file {0}: It is not currently available.", playlistItem.FileName);
                    return stream;
                }
                Logger.Write(this, LogLevel.Debug, "Failed to create stream from file {0}: {1}", playlistItem.FileName, Enum.GetName(typeof(Errors), stream.Errors));
            }
            Logger.Write(this, LogLevel.Warn, "All providers failed for file \"{0}\".", playlistItem.FileName);
            return BassStream.Empty;
        }

        public bool HasActiveStream(string fileName)
        {
            return BassOutputStream.Active.Any(stream => string.Equals(stream.FileName, fileName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
