using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Timers;
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Composition;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class SystemMediaTransportControlsBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        const int UPDATE_INTERVAL = 1000;

        public SystemMediaTransportControls TransportControls { get; private set; }

        public Timer Timer { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IArtworkProvider ArtworkProvider { get; private set; }

        public IUserInterface UserInterface { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public bool Enabled
        {
            get
            {
                return this.TransportControls != null;
            }
        }

        public override void InitializeComponent(ICore core)
        {
            if (SystemMediaTransportControlsBehaviourConfiguration.IsPlatformSupported)
            {
                this.PlaylistManager = core.Managers.Playlist;
                this.PlaybackManager = core.Managers.Playback;
                this.ArtworkProvider = core.Components.ArtworkProvider;
                this.UserInterface = core.Components.UserInterface;
                this.Configuration = core.Components.Configuration;
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    SystemMediaTransportControlsBehaviourConfiguration.SECTION,
                    SystemMediaTransportControlsBehaviourConfiguration.ENABLED_ELEMENT
                ).ConnectValue(value =>
                {
                    if (value)
                    {
                        this.Dispatch(this.Enable);
                    }
                    else
                    {
                        this.Disable();
                    }
                });
            }
            else
            {
                Logger.Write(this, LogLevel.Warn, "Platform is not supported.");
            }
            base.InitializeComponent(core);
        }

        public async Task Enable()
        {
            if (this.Enabled)
            {
                return;
            }
            //TODO: Bad .Result
            var window = await this.UserInterface.GetMainWindow().ConfigureAwait(false);
            if (window != null)
            {
                this.TransportControls = SystemMediaTransportControlsInterop.GetForWindow(window.Handle);
                if (this.TransportControls == null)
                {
                    Logger.Write(this, LogLevel.Warn, "Failed to construct for main window, falling back to global SMTC.");
                    this.TransportControls = this.GetGlobalSystemMediaTransportControls();
                }
            }
            else
            {
                Logger.Write(this, LogLevel.Warn, "Failed to determine the main window, falling back to global SMTC.");
                this.TransportControls = this.GetGlobalSystemMediaTransportControls();
            }
            this.TransportControls.IsEnabled = true;
            this.TransportControls.IsPlayEnabled = true;
            this.TransportControls.IsPauseEnabled = true;
            this.TransportControls.IsStopEnabled = true;
            //this.TransportControls.ShuffleEnabled = true;
            this.TransportControls.IsNextEnabled = true;
            this.TransportControls.IsPreviousEnabled = true;
            //this.TransportControls.IsFastForwardEnabled = false;
            //this.TransportControls.IsRewindEnabled = false;
            this.TransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
            this.TransportControls.ButtonPressed += this.OnButtonPressed;
            //this.TransportControls.AutoRepeatModeChangeRequested += this.OnAutoRepeatModeChangeRequested;
            //this.TransportControls.PlaybackPositionChangeRequested += this.OnPlaybackPositionChangeRequested;
            //this.TransportControls.PlaybackRateChangeRequested += this.OnPlaybackRateChangeRequested;
            //this.TransportControls.ShuffleEnabledChangeRequested += this.OnShuffleEnabledChangeRequested;
            //this.TransportControls.PropertyChanged += this.OnPropertyChanged;
            this.Timer = new Timer();
            this.Timer.Interval = UPDATE_INTERVAL;
            this.Timer.Elapsed += this.OnElapsed;
            this.Timer.Start();
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.Refresh();
            Logger.Write(this, LogLevel.Debug, "SystemMediaTransportControls enabled.");
        }

        public void Disable()
        {
            if (!this.Enabled)
            {
                return;
            }
            this.Timer.Stop();
            this.Timer.Elapsed -= this.OnElapsed;
            this.Timer.Dispose();
            this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            this.Timer = null;
            this.TransportControls.IsEnabled = false;
            this.TransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
            this.TransportControls.ButtonPressed -= this.OnButtonPressed;
            //this.TransportControls.AutoRepeatModeChangeRequested -= this.OnAutoRepeatModeChangeRequested;
            //this.TransportControls.PlaybackPositionChangeRequested -= this.OnPlaybackPositionChangeRequested;
            //this.TransportControls.PlaybackRateChangeRequested -= this.OnPlaybackRateChangeRequested;
            //this.TransportControls.ShuffleEnabledChangeRequested -= this.OnShuffleEnabledChangeRequested;
            //this.TransportControls.PropertyChanged -= this.OnPropertyChanged;
            this.TransportControls = null;
            Logger.Write(this, LogLevel.Debug, "SystemMediaTransportControls disabled.");
        }

        protected virtual SystemMediaTransportControls GetGlobalSystemMediaTransportControls()
        {
            var mediaPlayer = new MediaPlayer();
            return mediaPlayer.SystemMediaTransportControls;
        }

        public void Refresh()
        {
            this.RefreshState();
            this.RefreshStream();
            this.RefreshPosition();
        }

        protected virtual void RefreshState()
        {
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream == null)
            {
                this.TransportControls.PlaybackStatus = MediaPlaybackStatus.Closed;
            }
            else
            {
                if (outputStream.IsPaused)
                {
                    this.TransportControls.PlaybackStatus = MediaPlaybackStatus.Paused;
                }
                else if (outputStream.IsPlaying)
                {
                    this.TransportControls.PlaybackStatus = MediaPlaybackStatus.Playing;
                }
                else if (outputStream.IsStopped)
                {
                    this.TransportControls.PlaybackStatus = MediaPlaybackStatus.Stopped;
                }
            }
        }

        protected virtual void RefreshStream()
        {
            {
                var task = this.UpdateMusicProperties();
            }
            {
                var task = this.UpdateThumbnail();
            }
        }

        protected virtual void RefreshPosition()
        {
            //var properties = new SystemMediaTransportControlsTimelineProperties();
            //this.UpdateTimeline(properties);
            //this.TransportControls.UpdateTimelineProperties(properties);
        }

        protected virtual Task UpdateMusicProperties()
        {
            try
            {
                var outputStream = this.PlaybackManager.CurrentStream;
                var updater = this.TransportControls.DisplayUpdater;
                updater.ClearAll();
                updater.Type = MediaPlaybackType.Music;
                if (outputStream != null)
                {
                    var metaData = default(IDictionary<string, string>);
                    lock (outputStream.PlaylistItem.MetaDatas)
                    {
                        metaData = outputStream.PlaylistItem.MetaDatas.ToDictionary(
                            metaDataItem => metaDataItem.Name,
                            metaDataItem => metaDataItem.Value,
                            StringComparer.OrdinalIgnoreCase
                        );
                    }
                    this.Try(() => updater.MusicProperties.Title = metaData.GetValueOrDefault(CommonMetaData.Title) ?? string.Empty, this.ErrorHandler);
                    this.Try(() => updater.MusicProperties.Artist = metaData.GetValueOrDefault(CommonMetaData.Performer) ?? string.Empty, this.ErrorHandler);
                    this.Try(() => updater.MusicProperties.AlbumArtist = metaData.GetValueOrDefault(CommonMetaData.Artist) ?? string.Empty, this.ErrorHandler);
                    this.Try(() => updater.MusicProperties.AlbumTitle = metaData.GetValueOrDefault(CommonMetaData.Album) ?? string.Empty, this.ErrorHandler);
                    this.Try(() => updater.MusicProperties.TrackNumber = Convert.ToUInt32(metaData.GetValueOrDefault(CommonMetaData.Track)), this.ErrorHandler);
                    this.Try(() => updater.MusicProperties.AlbumTrackCount = Convert.ToUInt32(metaData.GetValueOrDefault(CommonMetaData.TrackCount)), this.ErrorHandler);
                    var genre = metaData.GetValueOrDefault(CommonMetaData.Genre);
                    if (!string.IsNullOrEmpty(genre))
                    {
                        this.Try(() => updater.MusicProperties.Genres.Add(genre), this.ErrorHandler);
                    }
                }
                updater.Update();
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to update music properties: {0}", e.Message);
            }
            return Task.CompletedTask;
        }

        protected virtual async Task UpdateThumbnail()
        {
            try
            {
                var outputStream = this.PlaybackManager.CurrentStream;
                var updater = this.TransportControls.DisplayUpdater;
                if (outputStream != null)
                {
                    var fileName = await this.ArtworkProvider.Find(outputStream.PlaylistItem, ArtworkType.FrontCover).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                    {
                        var stream = await this.GetThumbnail(fileName).ConfigureAwait(false);
                        updater.Thumbnail = RandomAccessStreamReference.CreateFromStream(stream);
                    }
                }
                updater.Update();
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to update music thumbnail: {0}", e.Message);
            }
        }

        protected virtual async Task<IRandomAccessStream> GetThumbnail(string fileName)
        {
            var output = new InMemoryRandomAccessStream();
            var file = await StorageFile.GetFileFromPathAsync(
                FileSystemHelper.GetAbsolutePath(Publication.StoragePath, fileName)
            );
            return await file.OpenReadAsync();
        }

        //protected virtual void UpdateTimeline(SystemMediaTransportControlsTimelineProperties properties)
        //{
        //    var outputStream = this.PlaybackManager.CurrentStream;
        //    if (outputStream == null)
        //    {
        //        properties.StartTime = TimeSpan.FromMilliseconds(0);
        //        properties.EndTime = TimeSpan.FromMilliseconds(0);
        //        properties.MinSeekTime = TimeSpan.FromSeconds(0);
        //        properties.MaxSeekTime = TimeSpan.FromSeconds(0);
        //        properties.Position = TimeSpan.FromMilliseconds(0);

        //        this.TransportControls.IsFastForwardEnabled = false;
        //        this.TransportControls.IsRewindEnabled = false;
        //    }
        //    else
        //    {
        //        properties.StartTime = TimeSpan.FromMilliseconds(0);
        //        properties.EndTime = TimeSpan.FromMilliseconds(outputStream.Length);
        //        properties.MinSeekTime = TimeSpan.FromMilliseconds(0);
        //        properties.MaxSeekTime = TimeSpan.FromMilliseconds(outputStream.Length);
        //        properties.Position = TimeSpan.FromMilliseconds(outputStream.Position);

        //        this.TransportControls.IsFastForwardEnabled = true;
        //        this.TransportControls.IsRewindEnabled = true;
        //    }
        //}

        protected virtual void OnButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            var task = default(Task);
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    task = this.Play();
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    task = this.Pause();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    task = this.Next();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    task = this.Previous();
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    task = this.Stop();
                    break;
            }
            this.RefreshState();
            Logger.Write(this, LogLevel.Debug, "Handled button press: {0}", Enum.GetName(typeof(SystemMediaTransportControlsButton), args.Button));
        }

        //protected virtual void OnAutoRepeatModeChangeRequested(SystemMediaTransportControls sender, AutoRepeatModeChangeRequestedEventArgs args)
        //{
        //    //Nothing to do.
        //}

        //protected virtual void OnPlaybackPositionChangeRequested(SystemMediaTransportControls sender, PlaybackPositionChangeRequestedEventArgs args)
        //{
        //    //Nothing to do.
        //}

        //protected virtual void OnPlaybackRateChangeRequested(SystemMediaTransportControls sender, PlaybackRateChangeRequestedEventArgs args)
        //{
        //    //Nothing to do.
        //}

        //protected virtual void OnShuffleEnabledChangeRequested(SystemMediaTransportControls sender, ShuffleEnabledChangeRequestedEventArgs args)
        //{
        //    //Nothing to do.
        //}

        //protected virtual void OnPropertyChanged(SystemMediaTransportControls sender, SystemMediaTransportControlsPropertyChangedEventArgs args)
        //{
        //    //Nothing to do.
        //}

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        protected virtual void OnElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                this.RefreshState();
                this.RefreshPosition();
            }
            catch
            {
                //Nothing can be done, never throw on background thread.
            }
        }

        protected virtual Task Play()
        {
            if (this.PlaybackManager.CurrentStream == null)
            {
                return this.PlaylistManager.Next();
            }
            else if (this.PlaybackManager.CurrentStream.IsPaused)
            {
                return this.PlaybackManager.CurrentStream.Resume();
            }
            else if (this.PlaybackManager.CurrentStream.IsStopped)
            {
                return this.PlaybackManager.CurrentStream.Play();
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual Task Pause()
        {
            if (this.PlaybackManager.CurrentStream != null)
            {
                if (this.PlaybackManager.CurrentStream.IsPaused)
                {
                    return this.PlaybackManager.CurrentStream.Resume();
                }
                else if (this.PlaybackManager.CurrentStream.IsPlaying)
                {
                    return this.PlaybackManager.CurrentStream.Pause();
                }
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual Task Next()
        {
            return this.PlaylistManager.Next();
        }

        protected virtual Task Previous()
        {
            return this.PlaylistManager.Previous();
        }

        protected virtual Task Stop()
        {
            return this.PlaybackManager.Stop();
        }

        private void ErrorHandler(Exception e)
        {
            Logger.Write(this, LogLevel.Error, "Failed to set music properties: {0}", e.Message);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return SystemMediaTransportControlsBehaviourConfiguration.GetConfigurationSections();
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
            this.Disable();
        }

        ~SystemMediaTransportControlsBehaviour()
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

        [Guid("ddb0472d-c911-4a1f-86d9-dc3d71a95f5a")]
        [InterfaceType(ComInterfaceType.InterfaceIsIInspectable)]
        public interface ISystemMediaTransportControlsInterop
        {
            int GetForWindow(IntPtr handle, ref Guid id, [MarshalAs(UnmanagedType.IUnknown)] out object systemMediaTransportControls);
        }

        public static class SystemMediaTransportControlsInterop
        {
            public static SystemMediaTransportControls GetForWindow(IntPtr handle)
            {
                var interop = (ISystemMediaTransportControlsInterop)WindowsRuntimeMarshal.GetActivationFactory(typeof(SystemMediaTransportControls));
                var id = new Guid("99FA3FF4-1742-42A6-902E-087D41F965EC");
                var systemMediaTransportControls = default(object);
                var result = interop.GetForWindow(handle, ref id, out systemMediaTransportControls);
                if (result != 0)
                {
                    return null;
                }
                return (SystemMediaTransportControls)systemMediaTransportControls;
            }
        }
    }
}
