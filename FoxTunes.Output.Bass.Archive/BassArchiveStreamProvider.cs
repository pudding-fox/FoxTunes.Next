using FoxTunes.Interfaces;
using ManagedBass;
using ManagedBass.ZipStream;
using System;
using System.Collections.Generic;

namespace FoxTunes
{
    public class BassArchiveStreamProvider : BassStreamProvider
    {
        public BassArchiveStreamProviderBehaviour ProviderBehaviour { get; private set; }

        public BassArchiveStreamPasswordBehaviour PasswordBehaviour { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.ProviderBehaviour = ComponentRegistry.Instance.GetComponent<BassArchiveStreamProviderBehaviour>();
            this.PasswordBehaviour = ComponentRegistry.Instance.GetComponent<BassArchiveStreamPasswordBehaviour>();
            base.InitializeComponent(core);
        }

        public override bool CanCreateStream(PlaylistItem playlistItem)
        {
            var fileName = default(string);
            var entryName = default(string);
            return ArchiveUtils.ParseUrl(playlistItem.FileName, out fileName, out entryName);
        }

        public override IBassStream CreateBasicStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, BassFlags flags)
        {
            var fileName = default(string);
            var entryName = default(string);
            if (!ArchiveUtils.ParseUrl(playlistItem.FileName, out fileName, out entryName))
            {
                //This shouldn't happen as CanCreateStream would have returned false.
                return BassStream.Empty;
            }
            var index = default(int);
            if (!ArchiveUtils.GetEntryIndex(fileName, entryName, out index))
            {
                //The associated entry was not found.
                return BassStream.Empty;
            }
        retry:
            var channelHandle = BassZipStream.CreateStream(fileName, index, Flags: flags);
            if (channelHandle == 0)
            {
                switch (ArchiveError.GetLastError())
                {
                    case ArchiveError.E_PASSWORD_REQUIRED:
                        Logger.Write(this, LogLevel.Warn, "Invalid password for \"{0}\".", fileName);
                        if (this.PasswordBehaviour != null)
                        {
                            var cancelled = this.PasswordBehaviour.WasCancelled(fileName);
                            this.PasswordBehaviour.Reset(fileName);
                            if (!cancelled)
                            {
                                goto retry;
                            }
                        }
                        break;
                }
                Logger.Write(this, LogLevel.Warn, "Failed to create archive stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
            }
            return this.CreateBasicStream(channelHandle, advice, flags);
        }

        public override IBassStream CreateInteractiveStream(PlaylistItem playlistItem, IEnumerable<IBassStreamAdvice> advice, bool immidiate, BassFlags flags)
        {
            var fileName = default(string);
            var entryName = default(string);
            if (!ArchiveUtils.ParseUrl(playlistItem.FileName, out fileName, out entryName))
            {
                //This shouldn't happen as CanCreateStream would have returned false.
                return BassStream.Empty;
            }
            var index = default(int);
            if (!ArchiveUtils.GetEntryIndex(fileName, entryName, out index))
            {
                //The associated entry was not found.
                return BassStream.Empty;
            }
        retry:
            var channelHandle = BassZipStream.CreateStream(fileName, index, Flags: flags);
            if (channelHandle == 0)
            {
                switch (ArchiveError.GetLastError())
                {
                    case ArchiveError.E_PASSWORD_REQUIRED:
                        Logger.Write(this, LogLevel.Warn, "Invalid password for \"{0}\".", fileName);
                        if (this.PasswordBehaviour != null)
                        {
                            var cancelled = this.PasswordBehaviour.WasCancelled(fileName);
                            this.PasswordBehaviour.Reset(fileName);
                            if (!cancelled)
                            {
                                goto retry;
                            }
                        }
                        break;
                }
                Logger.Write(this, LogLevel.Warn, "Failed to create archive stream: {0}", Enum.GetName(typeof(Errors), Bass.LastError));
            }
            return this.CreateInteractiveStream(channelHandle, advice, flags);
        }
    }
}
