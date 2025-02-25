using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [ComponentPriority(ComponentPriorityAttribute.LOW)]
    [ComponentDependency(Slot = ComponentSlots.Output)]
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class BassSkipSilenceStreamAdvisor : BassStreamAdvisor
    {
        //50ms
        const float WINDOW = 0.05f;

        public static readonly KeyLock<string> KeyLock = new KeyLock<string>(StringComparer.OrdinalIgnoreCase);

        public BassSkipSilenceStreamAdvisorBehaviour Behaviour { get; private set; }

        public IMetaDataManager MetaDataManager { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Behaviour = ComponentRegistry.Instance.GetComponent<BassSkipSilenceStreamAdvisorBehaviour>();
            this.MetaDataManager = core.Managers.MetaData;
            base.InitializeComponent(core);
        }

        public override void Advise(IBassStreamProvider provider, PlaylistItem playlistItem, IList<IBassStreamAdvice> advice, BassStreamUsageType type)
        {
            if (this.Behaviour == null || !this.Behaviour.Enabled)
            {
                return;
            }

            var leadIn = default(TimeSpan);
            var leadOut = default(TimeSpan);
            try
            {
                if (!this.TryGetSilence(provider, playlistItem, advice, out leadIn, out leadOut))
                {
                    return;
                }
                advice.Add(new BassSkipSilenceStreamAdvice(playlistItem.FileName, leadIn, leadOut));
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create stream advice for file \"{0}\": {1}", playlistItem.FileName, e.Message);
            }
        }

        protected virtual bool TryGetSilence(IBassStreamProvider provider, PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, out TimeSpan leadIn, out TimeSpan leadOut)
        {
            if (this.TryGetMetaData(this.Behaviour, playlistItem, out leadIn, out leadOut))
            {
                return true;
            }
            if (!this.TryCalculateSilence(provider, playlistItem, advice, out leadIn, out leadOut))
            {
                return false;
            }
            //Have to copy as cannot pass out parameter to lambda expression.
            var _leadIn = leadIn;
            var _leadOut = leadOut;
            this.Dispatch(() => this.UpdateMetaData(playlistItem, _leadIn, _leadOut));
            return true;
        }

        protected virtual Task UpdateMetaData(PlaylistItem playlistItem, TimeSpan leadIn, TimeSpan leadOut)
        {
            Logger.Write(this, LogLevel.Debug, "Updating lead in/out meta data for file \"{0}\".", playlistItem.FileName);

            var leadInMetaDataItem = default(MetaDataItem);
            var leadOutMetaDataItem = default(MetaDataItem);
            lock (playlistItem.MetaDatas)
            {
                var metaDatas = playlistItem.MetaDatas.ToDictionary();
                if (!metaDatas.TryGetValue(CustomMetaData.LeadIn, out leadInMetaDataItem))
                {
                    leadInMetaDataItem = new MetaDataItem(
                        CustomMetaData.LeadIn,
                        MetaDataItemType.Tag
                    );
                    playlistItem.MetaDatas.Add(leadInMetaDataItem);
                }
                if (!metaDatas.TryGetValue(CustomMetaData.LeadOut, out leadOutMetaDataItem))
                {
                    leadOutMetaDataItem = new MetaDataItem(
                        CustomMetaData.LeadOut,
                        MetaDataItemType.Tag
                    );
                    playlistItem.MetaDatas.Add(leadOutMetaDataItem);
                }
                leadInMetaDataItem.Value = string.Format("{0}:{1}", this.Behaviour.Threshold, leadIn);
                leadOutMetaDataItem.Value = string.Format("{0}:{1}", this.Behaviour.Threshold, leadOut);
            }
            return this.MetaDataManager.Save(
                new[] { playlistItem },
                new[] { CustomMetaData.LeadIn, CustomMetaData.LeadOut },
                MetaDataUpdateType.System,
                MetaDataUpdateFlags.None
            );
        }

        protected virtual bool TryCalculateSilence(IBassStreamProvider provider, PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, out TimeSpan leadIn, out TimeSpan leadOut)
        {
            if (provider.Flags.HasFlag(BassStreamProviderFlags.Serial))
            {
                Logger.Write(this, LogLevel.Debug, "Cannot calculate lead in/out for file \"{0}\": The provider does not support this action.", playlistItem.FileName);

                leadIn = default(TimeSpan);
                leadOut = default(TimeSpan);
                return false;
            }

            Logger.Write(this, LogLevel.Debug, "Attempting to calculate lead in/out for file \"{0}\".", playlistItem.FileName);

            var stream = provider.CreateBasicStream(playlistItem, advice, BassFlags.Decode | BassFlags.Byte);
            if (stream.IsEmpty)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create stream for file \"{0}\": {1}", playlistItem.FileName, Enum.GetName(typeof(Errors), Bass.LastError));

                leadIn = default(TimeSpan);
                leadOut = default(TimeSpan);
                return false;
            }
            try
            {
                var leadInBytes = this.GetLeadIn(stream, this.Behaviour.Threshold);
                if (leadInBytes == -1)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to calculate lead in for file \"{0}\": Track was considered silent.", playlistItem.FileName);

                    leadIn = default(TimeSpan);
                    leadOut = default(TimeSpan);
                    return false;
                }
                var leadOutBytes = this.GetLeadOut(stream, this.Behaviour.Threshold);
                if (leadOutBytes == -1)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to calculate lead out for file \"{0}\": Track was considered silent.", playlistItem.FileName);

                    leadIn = default(TimeSpan);
                    leadOut = default(TimeSpan);
                    return false;
                }
                leadIn = TimeSpan.FromSeconds(
                    Bass.ChannelBytes2Seconds(
                        stream.ChannelHandle,
                        leadInBytes
                    )
                );
                leadOut = TimeSpan.FromSeconds(
                    Bass.ChannelBytes2Seconds(
                        stream.ChannelHandle,
                        leadOutBytes
                    )
                );

                Logger.Write(this, LogLevel.Debug, "Successfully calculated lead in/out for file \"{0}\": {1}/{2}", playlistItem.FileName, leadIn, leadOut);

                return true;
            }
            finally
            {
                provider.FreeStream(stream.ChannelHandle);
            }
        }

        protected virtual long GetLeadIn(IBassStream stream, int threshold)
        {
            Logger.Write(this, LogLevel.Debug, "Attempting to calculate lead in for channel {0} with window of {1} seconds and threshold of {2}dB.", stream.ChannelHandle, WINDOW, threshold);

            var length = Bass.ChannelSeconds2Bytes(
                stream.ChannelHandle,
                WINDOW
            );
            var position = 0;
            if (!this.TrySetPosition(stream, position))
            {
                Logger.Write(this, LogLevel.Warn, "Failed to synchronize channel {0}, bad plugin? This is very expensive!", stream.ChannelHandle);

                //Track won't synchronize. MOD files seem to have this problem.                
                return -1;
            }
            do
            {
                var levels = new float[1];
                if (!Bass.ChannelGetLevel(stream.ChannelHandle, levels, WINDOW, LevelRetrievalFlags.Mono | LevelRetrievalFlags.RMS))
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to get levels for channel {0}: {1}", stream.ChannelHandle, Enum.GetName(typeof(Errors), Bass.LastError));

                    break;
                }
                var dB = levels[0] > 0 ? 20 * Math.Log10(levels[0]) : -1000;
                if (dB > threshold)
                {
                    //TODO: Sometimes this value is less than zero so clamp it.
                    //TODO: Some problem with BASS/ManagedBass, if you have exactly N bytes available call Bass.ChannelGetLevel with Length = Bass.ChannelBytesToSeconds(N) sometimes results in Errors.Ended.
                    //TODO: Nuts.
                    var leadIn = Math.Max(stream.Position - length, 0);

                    if (leadIn > 0)
                    {
                        Logger.Write(this, LogLevel.Debug, "Successfully calculated lead in for channel {0}: {1} bytes.", stream.ChannelHandle, leadIn);
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Debug, "Successfully calculated lead in for channel {0}: None.", stream.ChannelHandle);
                    }

                    return leadIn;
                }
            } while (true);
            //Track was silent?
            return -1;
        }

        protected virtual long GetLeadOut(IBassStream stream, int threshold)
        {
            Logger.Write(this, LogLevel.Debug, "Attempting to calculate lead out for channel {0} with window of {1} seconds and threshold of {2}dB.", stream.ChannelHandle, WINDOW, threshold);

            var length = Bass.ChannelSeconds2Bytes(
                stream.ChannelHandle,
                WINDOW
            );
            if (!this.TrySetPosition(stream, stream.Length - (length * 2)))
            {
                Logger.Write(this, LogLevel.Warn, "Failed to synchronize channel {0}, bad plugin? This is very expensive!", stream.ChannelHandle);

                //Track won't synchronize. MOD files seem to have this problem.
                return -1;
            }
            do
            {
                var levels = new float[1];
                if (!Bass.ChannelGetLevel(stream.ChannelHandle, levels, WINDOW, LevelRetrievalFlags.Mono | LevelRetrievalFlags.RMS))
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to get levels for channel {0}: {1}", stream.ChannelHandle, Enum.GetName(typeof(Errors), Bass.LastError));

                    break;
                }
                var dB = levels[0] > 0 ? 20 * Math.Log10(levels[0]) : -1000;
                if (dB > threshold)
                {
                    //TODO: Sometimes this value is less than zero so clamp it.
                    //TODO: Some problem with BASS/ManagedBass, if you have exactly N bytes available call Bass.ChannelGetLevel with Length = Bass.ChannelBytesToSeconds(N) sometimes results in Errors.Ended.
                    //TODO: Nuts.
                    var leadOut = Math.Max(stream.Length - stream.Position - length, 0);

                    if (leadOut > 0)
                    {
                        Logger.Write(this, LogLevel.Debug, "Successfully calculated lead out for channel {0}: {1} bytes.", stream.ChannelHandle, leadOut);
                    }
                    else
                    {
                        Logger.Write(this, LogLevel.Debug, "Successfully calculated lead out for channel {0}: None.", stream.ChannelHandle);
                    }

                    return leadOut;
                }
                if (!this.TrySetPosition(stream, stream.Position - (length * 2)))
                {
                    break;
                }
            } while (true);
            //Track was silent?
            return -1;
        }

        protected virtual bool TrySetPosition(IBassStream stream, long position)
        {
            return (stream.Position = position) == position;
        }

        protected virtual bool TryGetMetaData(BassSkipSilenceStreamAdvisorBehaviour behaviour, PlaylistItem playlistItem, out TimeSpan leadIn, out TimeSpan leadOut)
        {
            if (playlistItem.MetaDatas == null)
            {
                //This shouldn't happen.
                leadIn = default(TimeSpan);
                leadOut = default(TimeSpan);
                return false;
            }

            Logger.Write(typeof(BassSkipSilenceStreamAdvisor), LogLevel.Debug, "Attempting to fetch lead in/out for file \"{0}\" from meta data.", playlistItem.FileName);

            var leadInMetaDataItem = default(MetaDataItem);
            var leadOutMetaDataItem = default(MetaDataItem);
            lock (playlistItem.MetaDatas)
            {
                var metaDatas = playlistItem.MetaDatas.ToDictionary();
                metaDatas.TryGetValue(CustomMetaData.LeadIn, out leadInMetaDataItem);
                metaDatas.TryGetValue(CustomMetaData.LeadOut, out leadOutMetaDataItem);
                if (leadInMetaDataItem == null && leadOutMetaDataItem == null)
                {
                    Logger.Write(typeof(BassSkipSilenceStreamAdvisor), LogLevel.Debug, "Lead in/out meta data does not exist for file \"{0}\".", playlistItem.FileName);

                    leadIn = default(TimeSpan);
                    leadOut = default(TimeSpan);
                    return false;
                }
            }

            if (leadInMetaDataItem == null)
            {
                leadIn = TimeSpan.Zero;
            }
            else if (!this.TryParseDuration(behaviour, leadInMetaDataItem.Value, out leadIn))
            {
                Logger.Write(typeof(BassSkipSilenceStreamAdvisor), LogLevel.Debug, "Lead in meta data value \"{0}\" for file \"{1}\" is not valid.", leadInMetaDataItem.Value, playlistItem.FileName);

                leadIn = default(TimeSpan);
                leadOut = default(TimeSpan);
                return false;
            }

            if (leadOutMetaDataItem == null)
            {
                leadOut = TimeSpan.Zero;
            }
            else if (!this.TryParseDuration(behaviour, leadOutMetaDataItem.Value, out leadOut))
            {
                Logger.Write(typeof(BassSkipSilenceStreamAdvisor), LogLevel.Debug, "Lead out meta data value \"{0}\" for file \"{1}\" is not valid.", leadOutMetaDataItem.Value, playlistItem.FileName);

                leadIn = default(TimeSpan);
                leadOut = default(TimeSpan);
                return false;
            }

            Logger.Write(typeof(BassSkipSilenceStreamAdvisor), LogLevel.Debug, "Successfully fetched lead in/out from meta data for file \"{0}\": {1}/{2}", playlistItem.FileName, leadIn, leadOut);

            return true;
        }

        protected virtual bool TryParseDuration(BassSkipSilenceStreamAdvisorBehaviour behaviour, string value, out TimeSpan duration)
        {
            if (string.IsNullOrEmpty(value))
            {
                duration = TimeSpan.Zero;
                return false;
            }
            var parts = value.Split(new[] { ':' }, 2);
            if (parts.Length != 2)
            {
                duration = TimeSpan.Zero;
                return false;
            }
            var threshold = default(int);
            if (!int.TryParse(parts[0], out threshold) || behaviour.Threshold != threshold)
            {
                Logger.Write(typeof(BassSkipSilenceStreamAdvisor), LogLevel.Warn, "Ignoring lead in/out value \"{0}\": Invalid threshold.", parts[0]);

                duration = TimeSpan.Zero;
                return false;
            }
            if (!TimeSpan.TryParse(parts[1], out duration))
            {
                Logger.Write(typeof(BassSkipSilenceStreamAdvisor), LogLevel.Warn, "Ignoring lead in/out value \"{0}\": Invalid duration.", parts[1]);

                duration = TimeSpan.Zero;
                return false;
            }

            return true;
        }
    }
}
