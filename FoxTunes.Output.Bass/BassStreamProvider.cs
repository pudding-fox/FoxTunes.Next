using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    [ComponentPriority(ComponentPriorityAttribute.LOW)]
    public class BassStreamProvider : StandardComponent, IBassStreamProvider
    {
        public virtual IEnumerable<Type> SupportedInputs
        {
            get
            {
                return new[]
                {
                    typeof(IBassStreamInput)
                };
            }
        }

        public IBassOutput Output { get; private set; }

        public IBassStreamPipelineManager PipelineManager { get; private set; }

        public virtual BassStreamProviderFlags Flags
        {
            get
            {
                return BassStreamProviderFlags.None;
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.Output = core.Components.Output as IBassOutput;
            this.PipelineManager = ComponentRegistry.Instance.GetComponent<IBassStreamPipelineManager>();
            base.InitializeComponent(core);
        }

        public virtual bool CanCreateStream(PlaylistItem playlistItem)
        {
            return true;
        }

        public virtual IBassStream CreateBasicStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, BassFlags flags)
        {
            var fileName = this.GetFileName(playlistItem, advice);
            var channelHandle = Bass.CreateStream(fileName, 0, 0, flags);
            return this.CreateBasicStream(channelHandle, advice, flags);
        }

        protected virtual IBassStream CreateBasicStream(int channelHandle, IEnumerable<IBassStreamAdvice> advice, BassFlags flags)
        {
            if (channelHandle == 0)
            {
                Logger.Write(this, LogLevel.Debug, "Failed to create stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
                return BassStream.Error(this, Bass.LastError);
            }
            try
            {
                var stream = default(IBassStream);
                this.Wrap(channelHandle, advice, flags, out stream);
                if (stream == null)
                {
                    stream = new BassStream(this, channelHandle, Bass.ChannelGetLength(channelHandle, PositionFlags.Bytes), advice, flags);
                }
                return stream;
            }
            catch (BassException e)
            {
                return BassStream.Error(this, e.Error);
            }
        }

        public virtual IBassStream CreateInteractiveStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, bool immidiate, BassFlags flags)
        {
            var fileName = this.GetFileName(playlistItem, advice);
            var channelHandle = Bass.CreateStream(fileName, 0, 0, flags);
            return this.CreateInteractiveStream(channelHandle, advice, flags);
        }

        protected virtual IBassStream CreateInteractiveStream(int channelHandle, IEnumerable<IBassStreamAdvice> advice, BassFlags flags)
        {
            if (channelHandle == 0)
            {
                Logger.Write(this, LogLevel.Debug, "Failed to create stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
                return BassStream.Error(this, Bass.LastError);
            }
            try
            {
                var stream = default(IBassStream);
                this.Wrap(channelHandle, advice, flags, out stream);
                if (stream == null)
                {
                    stream = new BassStream(this, channelHandle, Bass.ChannelGetLength(channelHandle, PositionFlags.Bytes), advice, flags);
                }
                stream.AddSyncHandlers();
                return stream;
            }
            catch (BassException e)
            {
                return BassStream.Error(this, e.Error);
            }
        }

        protected virtual string GetFileName(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice)
        {
            foreach (var advisory in advice)
            {
                if (!string.IsNullOrEmpty(advisory.FileName))
                {
                    return advisory.FileName;
                }
            }
            return playlistItem.FileName;
        }

        protected virtual void Wrap(int channelHandle, IEnumerable<IBassStreamAdvice> advice, BassFlags flags, out IBassStream stream)
        {
            stream = default(IBassStream);
            foreach (var advisory in advice)
            {
                if (advisory.Wrap(this, channelHandle, advice, flags, out stream))
                {
                    if (stream.ChannelHandle != channelHandle)
                    {
                        Logger.Write(this, LogLevel.Debug, "Stream was wrapped by advice \"{0}\": {1} => {1}", advisory.GetType().Name, channelHandle, stream.ChannelHandle);
                        channelHandle = stream.ChannelHandle;
                    }
                }
            }
        }

        public virtual long GetPosition(int channelHandle)
        {
            return Bass.ChannelGetPosition(channelHandle, PositionFlags.Bytes);
        }

        public virtual void SetPosition(int channelHandle, long value)
        {
            BassUtils.OK(Bass.ChannelSetPosition(channelHandle, value, PositionFlags.Bytes));
        }

        public virtual void FreeStream(int channelHandle)
        {
            Logger.Write(this, LogLevel.Debug, "Freeing stream: {0}", channelHandle);
            Bass.StreamFree(channelHandle); //Not checking result code as it contains an error if the application is shutting down.
        }

        public bool IsDisposed { get; private set; }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (this.IsDisposed || !disposing)
            {
                return;
            }
            this.OnDisposing();
            this.IsDisposed = true;
        }

        protected virtual void OnDisposing()
        {
            //Nothing to do.
        }

        ~BassStreamProvider()
        {
            Logger.Write(this, LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        public class BassStreamProviderKey : IEquatable<BassStreamProviderKey>
        {
            public BassStreamProviderKey(string fileName, int channelHandle)
            {
                this.FileName = fileName;
                this.ChannelHandle = channelHandle;
            }

            public string FileName { get; private set; }

            public int ChannelHandle { get; private set; }

            public virtual bool Equals(BassStreamProviderKey other)
            {
                if (other == null)
                {
                    return false;
                }
                if (object.ReferenceEquals(this, other))
                {
                    return true;
                }
                if (!string.Equals(this.FileName, other.FileName, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                if (this.ChannelHandle != other.ChannelHandle)
                {
                    return false;
                }
                return true;
            }

            public override bool Equals(object obj)
            {
                return this.Equals(obj as BassStreamProviderKey);
            }

            public override int GetHashCode()
            {
                var hashCode = default(int);
                unchecked
                {
                    if (!string.IsNullOrEmpty(this.FileName))
                    {
                        hashCode += this.FileName.GetHashCode();
                    }
                    hashCode += this.ChannelHandle.GetHashCode();
                }
                return hashCode;
            }

            public static bool operator ==(BassStreamProviderKey a, BassStreamProviderKey b)
            {
                if ((object)a == null && (object)b == null)
                {
                    return true;
                }
                if ((object)a == null || (object)b == null)
                {
                    return false;
                }
                if (object.ReferenceEquals((object)a, (object)b))
                {
                    return true;
                }
                return a.Equals(b);
            }

            public static bool operator !=(BassStreamProviderKey a, BassStreamProviderKey b)
            {
                return !(a == b);
            }
        }
    }
}
