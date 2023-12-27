using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.Cd;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public class BassCdStreamProvider : BassStreamProvider
    {
        public static readonly object SyncRoot = new object();

        public override IEnumerable<Type> SupportedInputs
        {
            get
            {
                return new[]
                {
                    typeof(BassCdStreamInput)
                };
            }
        }

        public override BassStreamProviderFlags Flags
        {
            get
            {
                return base.Flags | BassStreamProviderFlags.Serial;
            }
        }

        public override bool CanCreateStream(PlaylistItem playlistItem)
        {
            var drive = default(int);
            var id = default(string);
            var track = default(int);
            return CdUtils.ParseUrl(playlistItem.FileName, out drive, out id, out track);
        }

        public override IBassStream CreateBasicStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, BassFlags flags)
        {
            var drive = default(int);
            var id = default(string);
            var track = default(int);
            var fileName = this.GetFileName(playlistItem, advice);
            if (!CdUtils.ParseUrl(fileName, out drive, out id, out track))
            {
                //This shouldn't happen as CanCreateStream would have returned false.
                return BassStream.Empty;
            }
            this.AssertDiscId(drive, id);
            var channelHandle = default(int);
            var input = this.GetInput();
            if (input != null)
            {
                return BassStream.Pending(this);
            }
            lock (SyncRoot)
            {
                if (BassCd.FreeOld)
                {
                    BassCd.FreeOld = false;
                }
                channelHandle = BassCd.CreateStream(drive, track, flags);
            }
            if (channelHandle == 0)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create CD stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
            }
            return this.CreateBasicStream(channelHandle, advice, flags);
        }

        public override IBassStream CreateInteractiveStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, bool immidiate, BassFlags flags)
        {
            var drive = default(int);
            var id = default(string);
            var track = default(int);
            var fileName = this.GetFileName(playlistItem, advice);
            if (!CdUtils.ParseUrl(fileName, out drive, out id, out track))
            {
                //This shouldn't happen as CanCreateStream would have returned false.
                return BassStream.Empty;
            }
            this.AssertDiscId(drive, id);
            var channelHandle = default(int);
            var input = this.GetInput();
            if (input != null)
            {
                if (immidiate)
                {
                    if (input.SetTrack(track, out channelHandle))
                    {
                        return this.CreateInteractiveStream(channelHandle, advice, flags);
                    }
                }
                else
                {
                    return BassStream.Pending(this);
                }
            }
            lock (SyncRoot)
            {
                if (immidiate)
                {
                    if (!BassCd.FreeOld)
                    {
                        BassCd.FreeOld = true;
                    }
                }
                else
                {
                    if (BassCd.FreeOld)
                    {
                        BassCd.FreeOld = false;
                    }
                }
                channelHandle = BassCd.CreateStream(drive, track, flags);
            }
            if (channelHandle == 0)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to create CD stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
            }
            return this.CreateInteractiveStream(channelHandle, advice, flags);
        }

        public override void FreeStream(int channelHandle)
        {
            var input = this.GetInput();
            if (input != null)
            {
                //Nothing to do, CD streams are re-cycled.
                return;
            }
            base.FreeStream(channelHandle);
        }

        protected virtual void AssertDiscId(int drive, string expected)
        {
            var actual = default(string);
            lock (SyncRoot)
            {
                actual = BassCd.GetID(drive, CDID.CDPlayer);
            }
            if (string.Equals(expected, actual, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
            throw new InvalidOperationException(string.Format("Found disc with identifier \"{0}\" when \"{1}\" was required.", actual, expected));
        }

        protected virtual BassCdStreamInput GetInput()
        {
            var input = default(BassCdStreamInput);
            if (this.Output != null && this.Output.IsStarted)
            {
                this.PipelineManager.WithPipeline(pipeline =>
                {
                    if (pipeline != null)
                    {
                        input = pipeline.Input as BassCdStreamInput;
                    }
                });
            }
            return input;
        }
    }
}
