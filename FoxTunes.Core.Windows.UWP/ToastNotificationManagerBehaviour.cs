using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using System.Linq;
using System.Threading.Tasks;
using System.Security;
using Windows.Storage;

namespace FoxTunes
{
    [ComponentDependency(Slot = ComponentSlots.UserInterface)]
    public class ToastNotificationManagerBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        const string GROUP = "87854E64-DD46-4C8C-8E29-B97FBF5265BD";

        const string TAG = "C4A36DDF-A8AC-49B6-AF8F-0A8A9F1541D0";

        public ToastNotifier ToastNotifier { get; private set; }

        public IPlaylistManager PlaylistManager { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IArtworkProvider ArtworkProvider { get; private set; }

        public IConfiguration Configuration { get; private set; }

        public bool Enabled
        {
            get
            {
                return this.ToastNotifier != null;
            }
        }

        public bool Popup { get; private set; }

        public bool LargeArtwork { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            if (ToastNotificationManagerBehaviourConfiguration.IsPlatformSupported)
            {
                this.PlaylistManager = core.Managers.Playlist;
                this.PlaybackManager = core.Managers.Playback;
                this.ArtworkProvider = core.Components.ArtworkProvider;
                this.Configuration = core.Components.Configuration;
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    ToastNotificationManagerBehaviourConfiguration.SECTION,
                    ToastNotificationManagerBehaviourConfiguration.ENABLED_ELEMENT
                ).ConnectValue(value =>
                {
                    if (value)
                    {
                        this.Enable();
                    }
                    else
                    {
                        this.Disable();
                        ToastNotificationHelper.Uninstall(false);
                    }
                });
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    ToastNotificationManagerBehaviourConfiguration.SECTION,
                    ToastNotificationManagerBehaviourConfiguration.POPUP_ELEMENT
                ).ConnectValue(value => this.Popup = value);
                this.Configuration.GetElement<BooleanConfigurationElement>(
                    ToastNotificationManagerBehaviourConfiguration.SECTION,
                    ToastNotificationManagerBehaviourConfiguration.LARGE_ARTWORK_ELEMENT
                ).ConnectValue(value => this.LargeArtwork = value);
            }
            else
            {
                Logger.Write(this, LogLevel.Warn, "Platform is not supported.");
            }
            base.InitializeComponent(core);
        }

        public void Enable()
        {
            if (this.Enabled)
            {
                return;
            }
            try
            {
                ToastNotificationHelper.Install();
                ToastNotificationHelper.Invoke(new Action(
                    () =>
                    {
                        if (Publication.IsPortable)
                        {
                            this.ToastNotifier = ToastNotificationManager.CreateToastNotifier(ToastNotificationHelper.ID);
                        }
                        else
                        {
                            this.ToastNotifier = ToastNotificationManager.CreateToastNotifier();
                        }
                    }
                ), null);
                this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to enable: {0}", e.Message);
            }
        }

        public void Disable()
        {
            if (!this.Enabled)
            {
                return;
            }
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            }
            try
            {
                ToastNotificationHelper.NotificationActivator.Disable();
                try
                {
                    ToastNotificationManager.History.Remove(TAG, GROUP, ToastNotificationHelper.ID);
                }
                catch
                {
                    //Nothing can be done.
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to disable: {0}", e.Message);
            }
            this.ToastNotifier = null;
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.ShowNotification);
        }

        protected virtual async Task ShowNotification()
        {
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream == null)
            {
                return;
            }
            try
            {
                var notification = await this.CreateNotification(outputStream).ConfigureAwait(false);
                ToastNotificationHelper.Invoke(new Action(() => this.ToastNotifier.Show(notification)), null);
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to show notification: {0}", e.Message);
            }
        }

        protected virtual async Task<ToastNotification> CreateNotification(IOutputStream outputStream)
        {
            var document = await this.CreateNotificationDocument(outputStream).ConfigureAwait(false);
            var notification = new ToastNotification(document);
            notification.Group = GROUP;
            notification.Tag = TAG;
            notification.SuppressPopup = !this.Popup;
            return notification;
        }

        protected virtual async Task<XmlDocument> CreateNotificationDocument(IOutputStream outputStream)
        {
            var metaData = default(IDictionary<string, string>);
            lock (outputStream.PlaylistItem.MetaDatas)
            {
                metaData = outputStream.PlaylistItem.MetaDatas.ToDictionary2();
            }
            var fileName = await this.ArtworkProvider.Find(
                outputStream.PlaylistItem,
                CommonImageTypes.FrontCover,
                ArtworkType.FrontCover
            ).ConfigureAwait(false);
            var hasArtwork = !string.IsNullOrEmpty(fileName) && File.Exists(fileName);
            var xml = default(string);
            if (hasArtwork)
            {
                if (this.LargeArtwork)
                {
                    xml = this.CreateNotificationXml_3(outputStream, metaData, fileName);
                }
                else
                {
                    xml = this.CreateNotificationXml_2(outputStream, metaData, fileName);
                }
            }
            else
            {
                xml = this.CreateNotificationXml_1(outputStream, metaData);
            }
            var document = new XmlDocument();
            document.LoadXml(xml);
            return document;
        }

        protected virtual string CreateNotificationXml_1(IOutputStream outputStream, IDictionary<string, string> metaData)
        {
            var title = default(string);
            var album = default(string);
            var artist = default(string);
            metaData.TryGetValue(CommonMetaData.Title, out title);
            metaData.TryGetValue(CommonMetaData.Album, out album);
            metaData.TryGetValue(CommonMetaData.Artist, out artist);
            return @"
<toast>
    <visual>
        <binding template=""ToastGeneric"">" +
(!string.IsNullOrEmpty(title) ? "<text>" + SecurityElement.Escape(title) + "</text>" : "<text>" + SecurityElement.Escape(Path.GetFileNameWithoutExtension(outputStream.FileName)) + "</text>") +
(!string.IsNullOrEmpty(album) ? "<text>" + SecurityElement.Escape(album) + "</text>" : string.Empty) +
(!string.IsNullOrEmpty(artist) ? "<text>" + SecurityElement.Escape(artist) + "</text>" : string.Empty) + @"
        </binding>
    </visual>
</toast>
";
        }

        protected virtual string CreateNotificationXml_2(IOutputStream outputStream, IDictionary<string, string> metaData, string fileName)
        {
            var title = default(string);
            var album = default(string);
            var artist = default(string);
            metaData.TryGetValue(CommonMetaData.Title, out title);
            metaData.TryGetValue(CommonMetaData.Album, out album);
            metaData.TryGetValue(CommonMetaData.Artist, out artist);
            return @"
<toast>
    <visual>
        <binding template=""ToastGeneric"">" +
(!string.IsNullOrEmpty(title) ? "<text>" + SecurityElement.Escape(title) + "</text>" : "<text>" + SecurityElement.Escape(Path.GetFileNameWithoutExtension(outputStream.FileName)) + "</text>") +
(!string.IsNullOrEmpty(album) ? "<text>" + SecurityElement.Escape(album) + "</text>" : string.Empty) +
(!string.IsNullOrEmpty(artist) ? "<text>" + SecurityElement.Escape(artist) + "</text>" : string.Empty) + @"
            <image
                placement=""appLogoOverride""
                hint-crop=""circle""
                src=""" + SecurityElement.Escape(this.GetFileUri(fileName)) + @""" />
        </binding>
    </visual>
</toast>
";
        }

        protected virtual string CreateNotificationXml_3(IOutputStream outputStream, IDictionary<string, string> metaData, string fileName)
        {
            var title = default(string);
            var album = default(string);
            var artist = default(string);
            metaData.TryGetValue(CommonMetaData.Title, out title);
            metaData.TryGetValue(CommonMetaData.Album, out album);
            metaData.TryGetValue(CommonMetaData.Artist, out artist);
            return @"
<toast>
    <visual>
        <binding template=""ToastGeneric"">" +
            (!string.IsNullOrEmpty(title) ? "<text>" + SecurityElement.Escape(title) + "</text>" : "<text>" + SecurityElement.Escape(Path.GetFileNameWithoutExtension(outputStream.FileName)) + "</text>") +
            (!string.IsNullOrEmpty(album) ? "<text>" + SecurityElement.Escape(album) + "</text>" : string.Empty) +
            (!string.IsNullOrEmpty(artist) ? "<text>" + SecurityElement.Escape(artist) + "</text>" : string.Empty) + @"
            <image
                placement=""inline""
                src=""" + SecurityElement.Escape(this.GetFileUri(fileName)) + @""" />
        </binding>
    </visual>
</toast>
";
        }

        protected virtual string GetFileUri(string fileName)
        {
            if (!Publication.IsPortable)
            {
                //This is ridiculous and there *has* to be a way to provide a ms-appdata:///localcache uri.
                //I can't find any examples of toasts containing images from %APPDATA%.
                fileName = fileName.Replace(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    //Why do I need to add Local???
                    Path.Combine(ApplicationData.Current.LocalCacheFolder.Path, "Local")
                );
            }
            else
            {
                fileName = Path.GetFullPath(fileName);
            }
            return new Uri(fileName).AbsoluteUri;
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return ToastNotificationManagerBehaviourConfiguration.GetConfigurationSections();
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

        ~ToastNotificationManagerBehaviour()
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
    }
}
