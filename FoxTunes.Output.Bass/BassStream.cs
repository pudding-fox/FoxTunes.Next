using FoxTunes.Interfaces;
using ManagedBass;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FoxTunes
{
    public class BassStream : BaseComponent, IBassStream
    {
        public const int ENDING_THRESHOLD = 5;

        private BassStream()
        {
            this.Syncs = new int[] { };
            this.Errors = Errors.OK;
        }

        public BassStream(IBassStreamProvider provider, int channelHandle, long length, IEnumerable<IBassStreamAdvice> advice, BassFlags flags) : this()
        {
            this.Provider = provider;
            this.ChannelHandle = channelHandle;
            this.Length = length;
            this.Advice = advice;
            this.Flags = flags;
        }

        public IBassStreamProvider Provider { get; private set; }

        public int ChannelHandle { get; private set; }

        public int[] Syncs { get; private set; }

        public long Length { get; private set; }

        public virtual long Position
        {
            get
            {
                if (this.Provider == null)
                {
                    return 0;
                }
                return this.Provider.GetPosition(this.ChannelHandle);
            }
            set
            {
                this.Provider.SetPosition(this.ChannelHandle, value);
                this.OnPositionChanged();
            }
        }

        protected virtual void OnPositionChanged()
        {
            if (this.IsEnded)
            {
                //Stream was resurrected.
                this.AddSyncHandlers();
                this.IsEnded = false;
            }
            if (this.Position > this.Length - Bass.ChannelSeconds2Bytes(this.ChannelHandle, ENDING_THRESHOLD))
            {
                this.OnEnding();
            }
        }

        public IEnumerable<IBassStreamAdvice> Advice { get; private set; }

        public BassFlags Flags { get; private set; }

        public Errors Errors { get; private set; }

        public bool IsInteractive
        {
            get
            {
                return this.Syncs != null && this.Syncs.Any();
            }
        }

        public bool IsEmpty
        {
            get
            {
                return this.ChannelHandle == 0;
            }
        }

        public bool IsPending
        {
            get
            {
                return this.Errors == Errors.Already;
            }
        }

        public bool IsEnded { get; private set; }

        public virtual void AddSyncHandlers()
        {
            this.Syncs = new int[]
            {
                 BassUtils.OK(Bass.ChannelSetSync(
                    this.ChannelHandle,
                    SyncFlags.Position,
                    this.Length - Bass.ChannelSeconds2Bytes(this.ChannelHandle, ENDING_THRESHOLD),
                    this.OnEnding
                )),
                BassUtils.OK(Bass.ChannelSetSync(
                    this.ChannelHandle,
                    SyncFlags.End,
                    0,
                    this.OnEnded
                ))
            };
        }

        public virtual void RemoveSyncHandlers()
        {
            if (this.Syncs != null)
            {
                foreach (var sync in this.Syncs)
                {
                    Bass.ChannelRemoveSync(this.ChannelHandle, sync);
                }
            }
        }

        protected virtual void OnEnding(int Handle, int Channel, int Data, IntPtr User)
        {
            //Critical: Don't block in this call back, it glitches playback.
            this.Dispatch(this.OnEnding);
        }

        protected virtual void OnEnding()
        {
            if (this.Ending != null)
            {
                this.Ending(this, EventArgs.Empty);
            }
        }

        public event EventHandler Ending;

        protected virtual void OnEnded(int Handle, int Channel, int Data, IntPtr User)
        {
            //Critical: Don't block in this call back, it glitches playback.
            this.Dispatch(this.OnEnded);
        }

        protected virtual void OnEnded()
        {
            this.RemoveSyncHandlers();
            this.IsEnded = true;
            if (this.Ended != null)
            {
                this.Ended(this, EventArgs.Empty);
            }
        }

        public event EventHandler Ended;

        public virtual bool CanReset
        {
            get
            {
                return true;
            }
        }

        public virtual void Reset()
        {
            this.Position = 0;
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
            this.RemoveSyncHandlers();
            if (this.Provider != null && this.ChannelHandle != 0)
            {
                this.Provider.FreeStream(this.ChannelHandle);
            }
        }

        ~BassStream()
        {
            try
            {
                this.Dispose(true);
            }
            catch
            {
                //Nothing can be done, never throw on GC thread.
            }
        }

        public static IBassStream Error(IBassStreamProvider provider, Errors errors)
        {
            return new BassStream()
            {
                Provider = provider,
                Errors = errors
            };
        }

        public static IBassStream Pending(IBassStreamProvider provider)
        {
            return new BassStream()
            {
                Provider = provider,
                Errors = Errors.Already
            };
        }

        public static IBassStream Empty
        {
            get
            {
                return new BassStream();
            }
        }
    }
}
