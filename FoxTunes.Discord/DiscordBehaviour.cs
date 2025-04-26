using Discord.Sharp;
using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace FoxTunes
{
    [WindowsUserInterfaceDependency]
    public class DiscordBehaviour : StandardBehaviour, IConfigurableComponent, IDisposable
    {
        public static readonly int INTERVAL = 1000;

        static DiscordBehaviour()
        {
            Loader.Load("discord-rpc.dll");
            Loader.Load("discord");
        }

        public const string CLIENT_ID = "1357689312660946984";

        public global::System.Timers.Timer Timer { get; private set; }

        public IPlaybackManager PlaybackManager { get; private set; }

        public IScriptingRuntime ScriptingRuntime { get; private set; }

        public IScriptingContext ScriptingContext { get; private set; }

        public IErrorEmitter ErrorEmitter { get; private set; }

        public IArtworkProvider ArtworkProvider { get; private set; }

        public ImageResizer ImageResizer { get; private set; }

        public IConfiguration Configuration { get; private set; }

        private string _BunnyApiKey { get; set; }

        public string BunnyApiKey
        {
            get
            {
                return this._BunnyApiKey;
            }
            set
            {
                this._BunnyApiKey = value;
                this.OnBunnyApiKeyChanged();
            }
        }

        protected virtual void OnBunnyApiKeyChanged()
        {
            var task = this.Refresh();
            if (this.BunnyApiKeyChanged != null)
            {
                this.BunnyApiKeyChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("BunnyApiKey");
        }

        public event EventHandler BunnyApiKeyChanged;

        private string _BunnyUploadUrl { get; set; }

        public string BunnyUploadUrl
        {
            get
            {
                return this._BunnyUploadUrl;
            }
            set
            {
                this._BunnyUploadUrl = value;
                this.OnBunnyUploadUrlChanged();
            }
        }

        protected virtual void OnBunnyUploadUrlChanged()
        {
            var task = this.Refresh();
            if (this.BunnyUploadUrlChanged != null)
            {
                this.BunnyUploadUrlChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("BunnyUploadUrl");
        }

        public event EventHandler BunnyUploadUrlChanged;

        private string _BunnyDownloadUrl { get; set; }

        public string BunnyDownloadUrl
        {
            get
            {
                return this._BunnyDownloadUrl;
            }
            set
            {
                this._BunnyDownloadUrl = value;
                this.OnBunnyDownloadUrlChanged();
            }
        }

        protected virtual void OnBunnyDownloadUrlChanged()
        {
            var task = this.Refresh();
            if (this.BunnyDownloadUrlChanged != null)
            {
                this.BunnyDownloadUrlChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("BunnyDownloadUrl");
        }

        public event EventHandler BunnyDownloadUrlChanged;

        private string _StateScript { get; set; }

        public string StateScript
        {
            get
            {
                return this._StateScript;
            }
            set
            {
                this._StateScript = value;
                this.OnStateScriptChanged();
            }
        }

        protected virtual void OnStateScriptChanged()
        {
            var task = this.Refresh();
            if (this.StateScriptChanged != null)
            {
                this.StateScriptChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("StateScript");
        }

        public event EventHandler StateScriptChanged;

        private string _DetailsScript { get; set; }

        public string DetailsScript
        {
            get
            {
                return this._DetailsScript;
            }
            set
            {
                this._DetailsScript = value;
                this.OnDetailsScriptChanged();
            }
        }

        protected virtual void OnDetailsScriptChanged()
        {
            var task = this.Refresh();
            if (this.DetailsScriptChanged != null)
            {
                this.DetailsScriptChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("DetailsScript");
        }

        public event EventHandler DetailsScriptChanged;

        public bool CanUpload
        {
            get
            {
                return
                    !string.IsNullOrEmpty(this.BunnyApiKey) &&
                    !string.IsNullOrEmpty(this.BunnyUploadUrl) &&
                    !string.IsNullOrEmpty(this.BunnyDownloadUrl);
            }
        }

        public override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.ScriptingRuntime = core.Components.ScriptingRuntime;
            this.ScriptingContext = this.ScriptingRuntime.CreateContext();
            this.ErrorEmitter = core.Components.ErrorEmitter;
            this.ArtworkProvider = core.Components.ArtworkProvider;
            this.ImageResizer = ComponentRegistry.Instance.GetComponent<ImageResizer>();
            this.Configuration = core.Components.Configuration;
            this.Configuration.GetElement<TextConfigurationElement>(
                DiscordBehaviourConfiguration.SECTION,
                DiscordBehaviourConfiguration.BUNNY_API_KEY
            ).ConnectValue(value => this.BunnyApiKey = value);
            this.Configuration.GetElement<TextConfigurationElement>(
                DiscordBehaviourConfiguration.SECTION,
                DiscordBehaviourConfiguration.BUNNY_UPLOAD_URL
            ).ConnectValue(value => this.BunnyUploadUrl = value);
            this.Configuration.GetElement<TextConfigurationElement>(
                DiscordBehaviourConfiguration.SECTION,
                DiscordBehaviourConfiguration.BUNNY_DOWNLOAD_URL
            ).ConnectValue(value => this.BunnyDownloadUrl = value);
            this.Configuration.GetElement<TextConfigurationElement>(
                DiscordBehaviourConfiguration.SECTION,
                DiscordBehaviourConfiguration.STATE_SCRIPT
            ).ConnectValue(value => this.StateScript = value);
            this.Configuration.GetElement<TextConfigurationElement>(
                DiscordBehaviourConfiguration.SECTION,
                DiscordBehaviourConfiguration.DETAILS_SCRIPT
            ).ConnectValue(value => this.DetailsScript = value);
            this.Configuration.GetElement<BooleanConfigurationElement>(
                DiscordBehaviourConfiguration.SECTION,
                DiscordBehaviourConfiguration.ENABLED
            ).ConnectValue(value =>
            {
                if (value)
                {
                    this.Enable();
                }
                else
                {
                    this.Disable();
                }
            });
            base.InitializeComponent(core);
        }

        public bool Enabled { get; private set; }

        public void Enable()
        {
            if (this.Enabled)
            {
                return;
            }
            try
            {
                DiscordManager.Create(CLIENT_ID);
                this.Timer = new global::System.Timers.Timer();
                this.Timer.Interval = INTERVAL;
                this.Timer.AutoReset = false;
                this.Timer.Elapsed += this.OnElapsed;
                this.Timer.Start();
                this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
                this.Enabled = true;
                var task = this.Refresh();
            }
            catch (Exception e)
            {
                this.ErrorEmitter.Send(this, e);
                this.Disable();
            }
        }

        public void Disable()
        {
            if (!this.Enabled)
            {
                return;
            }
            this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            if (this.Timer != null)
            {
                this.Timer.Stop();
                this.Timer.Elapsed -= this.OnElapsed;
                this.Timer.Dispose();
                this.Timer = null;
            }
            DiscordManager.Free();
            this.Enabled = false;
        }

        protected virtual void OnElapsed(object sender, ElapsedEventArgs e)
        {
            DiscordManager.RunCallbacks();
        }

        public Task Refresh()
        {
            if (this.PlaybackManager.CurrentStream != null)
            {
                return this.UpdateActivity(this.PlaybackManager.CurrentStream);
            }
            else
            {
                this.ClearActivity();
#if NET40
                return TaskEx.FromResult(false);
#else
                return Task.CompletedTask;
#endif
            }
        }

        protected virtual async Task UpdateActivity(IOutputStream outputStream)
        {
            var state = this.GetState(outputStream);
            var details = this.GetDetails(outputStream);
            var largeImageText = this.GetLargeImageText(outputStream);
            var largeImageKey = await this.GetLargeImageKey(outputStream).ConfigureAwait(false);
            var smallImageText = this.GetSmallImageText(outputStream);
            var smallImageKey = this.GetSmallImageKey(outputStream);
            DiscordManager.UpdatePresence(state, details, smallImageText, smallImageKey, largeImageText, largeImageKey);
        }

        protected virtual string GetState(IOutputStream outputStream)
        {
            var runner = new PlaylistItemScriptRunner(
                this.ScriptingContext,
                outputStream != null ? outputStream.PlaylistItem : null,
                this.StateScript
            );
            runner.Prepare();
            return Convert.ToString(runner.Run());
        }

        protected virtual string GetDetails(IOutputStream outputStream)
        {
            var runner = new PlaylistItemScriptRunner(
                this.ScriptingContext,
                outputStream != null ? outputStream.PlaylistItem : null,
                this.DetailsScript
            );
            runner.Prepare();
            return Convert.ToString(runner.Run());
        }

        protected virtual string GetLargeImageText(IOutputStream outputStream)
        {
            return null;
        }

        protected virtual async Task<string> GetLargeImageKey(IOutputStream outputStream)
        {
            if (!this.CanUpload)
            {
                Logger.Write(this, LogLevel.Debug, "Skipping file upload, missing settings.");
                return null;
            }
            try
            {
                var fileName = await this.ArtworkProvider.Find(outputStream.PlaylistItem, CommonImageTypes.FrontCover, ArtworkType.FrontCover).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(fileName) && File.Exists(fileName))
                {
                    fileName = this.GetThumbnail(fileName);
                    var location = await this.Upload(fileName);
                    return location;
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Error, "Failed to fetch or upload image: {0}", e.Message);
            }
            return null;
        }

        protected virtual string GetSmallImageText(IOutputStream outputStream)
        {
            return null;
        }

        protected virtual string GetSmallImageKey(IOutputStream outputStream)
        {
            //return "https://ft-thumbs.b-cdn.net/Square310x310Logo.scale-200.png";
            return null;
        }

        protected virtual string GetThumbnail(string fileName)
        {
            const int WIDTH = 512;
            const int HEIGHT = 512;
            return this.ImageResizer.Resize(fileName, WIDTH, HEIGHT, false);
        }

        protected virtual async Task<string> Upload(string fileName)
        {
            var storageZoneFileName = this.GetStorageZoneFileName(fileName);
            var exists = await this.Exists(fileName, storageZoneFileName).ConfigureAwait(false);
            if (!exists)
            {
                var request = await this.GetUploadRequest(fileName, storageZoneFileName).ConfigureAwait(false);
                var response = await this.GetResponse(request);
            }
            var url = string.Concat(this.BunnyDownloadUrl, "/", storageZoneFileName);
            return url;
        }

        protected virtual string GetStorageZoneFileName(string fileName)
        {
            using (var sha1 = new SHA1Managed())
            {
                var builder = new StringBuilder();
                var sequence = sha1.ComputeHash(Encoding.UTF8.GetBytes(fileName));
                foreach (var element in sequence)
                {
                    builder.Append(element.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        protected virtual async Task<bool> Exists(string fileName, string storageZoneFileName)
        {
            var request = this.GetExistsRequest(fileName, storageZoneFileName);
            try
            {
                var response = await this.GetResponse(request);
                return true;
            }
            catch
            {
                Logger.Write(this, LogLevel.Debug, "File does not exists: {0}", storageZoneFileName);
                return false;
            }
        }

        protected virtual HttpWebRequest GetExistsRequest(string fileName, string storageZoneFileName)
        {
            var accessKey = this.BunnyApiKey;
            var url = string.Concat(this.BunnyDownloadUrl, "/", storageZoneFileName);
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.ContentType = "application/octet-stream";
            request.Headers.Add("AccessKey", accessKey);

            return request;
        }

        protected virtual async Task<HttpWebRequest> GetUploadRequest(string fileName, string storageZoneFileName)
        {
            var accessKey = this.BunnyApiKey;
            var url = string.Concat(this.BunnyUploadUrl, "/", storageZoneFileName);
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "PUT";
            request.ContentType = "application/octet-stream";
            request.Headers.Add("AccessKey", accessKey);

            using (var fileStream = File.OpenRead(fileName))
            {
                using (var requestStream = await request.GetRequestStreamAsync().ConfigureAwait(false))
                {
                    await fileStream.CopyToAsync(requestStream);
                }
            }

            return request;
        }

        protected virtual async Task<HttpWebResponse> GetResponse(HttpWebRequest request)
        {
            var response = (HttpWebResponse)await request.GetResponseAsync().ConfigureAwait(false);
            using (var stream = response.GetResponseStream())
            {
                using (var reader = new StreamReader(stream))
                {
                    var responseString = reader.ReadToEnd();
                    //TODO: Logging?
                }
            }
            return response;
        }


        protected virtual void ClearActivity()
        {
            DiscordManager.ClearPresence();
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return DiscordBehaviourConfiguration.GetConfigurationSections();
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

        ~DiscordBehaviour()
        {
            Logger.Write(this.GetType(), LogLevel.Error, "Component was not disposed: {0}", this.GetType().Name);
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
