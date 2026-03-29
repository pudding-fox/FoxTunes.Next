using FoxTunes.Interfaces;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace FoxTunes.ViewModel
{
    public class AITrack : ConfigurableViewModelBase
    {
        private Tuple<string, string> _ArtistAndTitle { get; set; }

        public Tuple<string, string> ArtistAndTitle
        {
            get
            {
                return this._ArtistAndTitle;
            }
            set
            {
                if (this._ArtistAndTitle == null && value == null)
                {
                    return;
                }
                else if (this._ArtistAndTitle != null || value != null)
                {
                    //Nothing to do.
                }
                else if (this._ArtistAndTitle.Equals(value))
                {
                    return;
                }
                this._ArtistAndTitle = value;
                this.OnArtistAndTitleChanged();
            }
        }

        protected virtual void OnArtistAndTitleChanged()
        {
            if (this.ArtistAndTitle != null && !string.IsNullOrEmpty(this.ArtistAndTitle.Item1) && !string.IsNullOrEmpty(this.ArtistAndTitle.Item2))
            {
                this.Dispatch(() => this.Refresh(this.ArtistAndTitle.Item1, this.ArtistAndTitle.Item2));
            }
            if (this.ArtistAndTitleChanged != null)
            {
                this.ArtistAndTitleChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("ArtistAndTitle");
        }

        public event EventHandler ArtistAndTitleChanged;

        private string _Content { get; set; }

        public string Content
        {
            get
            {
                return this._Content;
            }
            set
            {
                this._Content = value;
                this.OnContentChanged();
            }
        }

        protected virtual void OnContentChanged()
        {
            if (this.ContentChanged != null)
            {
                this.ContentChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("Content");
        }

        public event EventHandler ContentChanged;

        private string _StatusMessage { get; set; }

        public virtual string StatusMessage
        {
            get
            {
                return this._StatusMessage;
            }
            set
            {
                this._StatusMessage = value;
                this.OnStatusMessageChanged();
            }
        }

        protected virtual void OnStatusMessageChanged()
        {
            this.OnHasStatusMessageChanged();
            if (this.StatusMessageChanged != null)
            {
                this.StatusMessageChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("StatusMessage");
        }

        public event EventHandler StatusMessageChanged;

        public bool HasStatusMessage
        {
            get
            {
                return !string.IsNullOrEmpty(this.StatusMessage);
            }
        }

        protected virtual void OnHasStatusMessageChanged()
        {
            if (this.HasStatusMessageChanged != null)
            {
                this.HasStatusMessageChanged(this, EventArgs.Empty);
            }
            this.OnPropertyChanged("HasStatusMessage");
        }

        public event EventHandler HasStatusMessageChanged;

        public IPlaybackManager PlaybackManager { get; private set; }

        public ILibraryBrowser LibraryBrowser { get; private set; }

        public IAIRuntime Runtime { get; private set; }

        public TextConfigurationElement PromptTemplate { get; private set; }

        protected override void OnConfigurationChanged()
        {
            if (this.Configuration != null)
            {
                this.PromptTemplate = this.Configuration.GetElement<TextConfigurationElement>(
                    AITrackConfiguration.SECTION,
                    AITrackConfiguration.PROMPT_TEMPLATE
                );
            }
            base.OnConfigurationChanged();
        }

        protected override void InitializeComponent(ICore core)
        {
            this.PlaybackManager = core.Managers.Playback;
            this.PlaybackManager.CurrentStreamChanged += this.OnCurrentStreamChanged;
            this.LibraryBrowser = core.Components.LibraryBrowser;
            this.Runtime = core.Components.AIRuntime;
            base.InitializeComponent(core);
        }

        protected virtual void OnCurrentStreamChanged(object sender, EventArgs e)
        {
            //Critical: Don't block in this event handler, it causes a deadlock.
            this.Dispatch(this.Refresh);
        }

        public virtual Task Refresh()
        {
            if (this.Runtime == null)
            {
                return Windows.Invoke(() => this.StatusMessage = "This feature requires an AI provider plugin.");
            }
            var outputStream = this.PlaybackManager.CurrentStream;
            if (outputStream != null)
            {
                var fileData = default(IFileData);
                if (outputStream.PlaylistItem.LibraryItem_Id.HasValue)
                {
                    fileData = this.LibraryBrowser.Get(outputStream.PlaylistItem.LibraryItem_Id.Value);
                }
                else
                {
                    fileData = outputStream.PlaylistItem;
                }
                this.Refresh(fileData);
            }
#if NET40
            return TaskEx.FromResult(false);
#else
            return Task.CompletedTask;
#endif
        }

        protected virtual void Refresh(IFileData fileData)
        {
            lock (fileData.MetaDatas)
            {
                var artist = default(string);
                var title = default(string);
                {
                    var metaDataItem = fileData.MetaDatas.FirstOrDefault(_metaDataItem => string.Equals(_metaDataItem.Name, CommonMetaData.Artist) && _metaDataItem.Type == MetaDataItemType.Tag);
                    if (metaDataItem != null)
                    {
                        artist = metaDataItem.Value;
                    }
                }
                {
                    var metaDataItem = fileData.MetaDatas.FirstOrDefault(_metaDataItem => string.Equals(_metaDataItem.Name, CommonMetaData.Title) && _metaDataItem.Type == MetaDataItemType.Tag);
                    if (metaDataItem != null)
                    {
                        title = metaDataItem.Value;
                    }
                }
                this.ArtistAndTitle = new Tuple<string, string>(artist, title);
            }
        }

        protected virtual async Task Refresh(string artist, string title)
        {
            Logger.Write(this, LogLevel.Debug, "Cleating AI context.");
            await Windows.Invoke(() => this.StatusMessage = Strings.AITrack_Loading).ConfigureAwait(false);
            await Windows.Invoke(() => this.Content = null);
            try
            {
                using (var context = this.Runtime.CreateContext())
                {
                    var store = context.CreateResponseStore();
                    var attempt = 0;
                    var prompt = string.Format(this.PromptTemplate.Value, title, artist);
                retry:
                    Logger.Write(this, LogLevel.Debug, "Sending request to AI: {0}", prompt);
                    var result = default(string);
                    try
                    {
                        result = await store.Create(prompt, CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to get response from AI: {0}", e.Message);
                        throw;
                    }
                    Logger.Write(this, LogLevel.Debug, "Response from AI: {0}", result);
                    var content = default(string);
                    try
                    {
                        content = await this.GetContentFromResponse(result).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Logger.Write(this, LogLevel.Warn, "Failed to extract content from the response: {0}", e.Message);
                    }
                    if (string.IsNullOrEmpty(result))
                    {
                        if (attempt++ < 5)
                        {
                            Logger.Write(this, LogLevel.Debug, "Will retry.");
                            await Task.Delay(1000).ConfigureAwait(false);
                            goto retry;
                        }
                        else
                        {
                            Logger.Write(this, LogLevel.Debug, "Timed out.");
                            return;
                        }
                    }
                    await Windows.Invoke(() => this.Content = content);
                    await Windows.Invoke(() => this.StatusMessage = default(string)).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                await Windows.Invoke(() => this.StatusMessage = e.Message).ConfigureAwait(false);
            }
        }

        protected virtual async Task<string> GetContentFromResponse(string response)
        {
            using (var reader = new StringReader(response))
            {
                var line = default(string);
                var foundHeader = default(bool);
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (line.StartsWith("```"))
                    {
                        foundHeader = true;
                        break;
                    }
                }
                if (!foundHeader)
                {
                    return default(string);
                }
                var builder = new StringBuilder();
                var foundFooter = default(bool);
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    if (line.StartsWith("```"))
                    {
                        foundFooter = true;
                        break;
                    }
                    builder.AppendLine(line);
                }
                if (!foundFooter)
                {
                    return default(string);
                }
                return builder.ToString();
            }
        }

        protected override void OnDisposing()
        {
            if (this.PlaybackManager != null)
            {
                this.PlaybackManager.CurrentStreamChanged -= this.OnCurrentStreamChanged;
            }
            base.OnDisposing();
        }

        protected override Freezable CreateInstanceCore()
        {
            return new AITrack();
        }
    }
}
