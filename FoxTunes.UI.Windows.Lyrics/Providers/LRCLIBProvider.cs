using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TinyJson;

namespace FoxTunes
{
    [Component(ID)]
    [WindowsUserInterfaceDependency]
    public class LRCLIBProvider : LyricsProvider, IConfigurableComponent
    {
        public const string ID = "2C54A300-DD63-49BD-B709-DAE6F6C10018";

        public const string BASE_URL = "https://lrclib.net/api";

        public LRCLIBProvider() : base(ID, Strings.LRCLIB)
        {

        }

        public IConfiguration Configuration { get; private set; }

        public TextConfigurationElement BaseUrl { get; private set; }

        public override void InitializeComponent(ICore core)
        {
            this.Configuration = core.Components.Configuration;
            this.BaseUrl = this.Configuration.GetElement<TextConfigurationElement>(
                LRCLIBProviderConfiguration.SECTION,
                LRCLIBProviderConfiguration.BASE_URL
            );
            base.InitializeComponent(core);
        }

        public override string None
        {
            get
            {
                return "none (" + this.Name + ", " + Publication.Version + ")";
            }
        }

        public override async Task<LyricsResult> Lookup(IFileData fileData)
        {
            Logger.Write(this, LogLevel.Debug, "Getting track information for file \"{0}\"..", fileData.FileName);
            var artist = default(string);
            var song = default(string);
            var duration = default(int);
            if (!this.TryGetLookup(fileData, out artist, out song, out duration))
            {
                Logger.Write(this, LogLevel.Warn, "Failed to get track information: The required meta data was not found.");
                return LyricsResult.Fail;
            }
            Logger.Write(this, LogLevel.Debug, "Got track information: Artist = \"{0}\", Song = \"{1}\".", artist, song);
            var result = default(IDictionary<string, string>);
            try
            {
                Logger.Write(this, LogLevel.Debug, "Searching for match..");
                result = await this.Lookup(artist, song, duration).ConfigureAwait(false);
                if (result != null)
                {
                    Logger.Write(this, LogLevel.Debug, "Got match, fetching lyrics..");
                    var plainLyrics = default(string);
                    if (result.TryGetValue("plainLyrics", out plainLyrics) && !string.IsNullOrEmpty(plainLyrics))
                    {
                        Logger.Write(this, LogLevel.Debug, "Success.");
                        return new LyricsResult(plainLyrics);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Write(this, LogLevel.Warn, "Failed to fetch lyrics: {0}", e.Message);
            }
            finally
            {
                //Save the LyricsRelease tag (either the actual id or none).
                var id = default(string);
                if (result != null && result.TryGetValue("id", out id) && !string.IsNullOrEmpty(id))
                {
                    await this.SaveMetaData(fileData, id).ConfigureAwait(false);
                }
                else
                {
                    await this.SaveMetaData(fileData, this.None).ConfigureAwait(false);
                }
            }
            return LyricsResult.Fail;
        }

        protected virtual async Task<IDictionary<string, string>> Lookup(string artist, string song, int duration)
        {
            var url = this.GetUrl(artist, song, duration);
            Logger.Write(this, LogLevel.Debug, "Querying the API: {0}", url);
            var request = WebRequestFactory.Create(url);
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Logger.Write(this, LogLevel.Warn, "Status code does not indicate success.");
                    return null;
                }
                using (var stream = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var json = await reader.ReadToEndAsync().ConfigureAwait(false);
                        var result = JSONParser.FromJson<Dictionary<string, string>>(json);
                        if (result == null)
                        {
                            var results = JSONParser.FromJson<List<Dictionary<string, string>>>(json);
                            if (results != null)
                            {
                                result = results.FirstOrDefault();
                            }
                        }
                        return result;
                    }
                }
            }
        }

        protected virtual string GetUrl(string artist, string song, int duration)
        {
            var baseUrl = this.BaseUrl.Value;
            if (string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = BASE_URL;
            }
            var builder = new StringBuilder();
            builder.Append(baseUrl);
            builder.Append("/search?");
            builder.Append(UrlHelper.GetParameters(new Dictionary<string, string>()
            {
                { "artist_name", artist },
                { "track_name", song },
                { "duration", Convert.ToString(duration) }
            }));
            return builder.ToString();
        }

        protected virtual bool TryGetLookup(IFileData fileData, out string artist, out string song, out int duration)
        {
            artist = default(string);
            song = default(string);
            duration = default(int);
            lock (fileData.MetaDatas)
            {
                foreach (var metaDataItem in fileData.MetaDatas)
                {
                    if (string.Equals(metaDataItem.Name, CommonMetaData.Artist, StringComparison.OrdinalIgnoreCase))
                    {
                        artist = metaDataItem.Value;
                    }
                    else if (string.Equals(metaDataItem.Name, CommonMetaData.Title, StringComparison.OrdinalIgnoreCase))
                    {
                        song = metaDataItem.Value;
                    }
                    else if (string.Equals(metaDataItem.Name, CommonProperties.Duration, StringComparison.OrdinalIgnoreCase))
                    {
                        if (int.TryParse(metaDataItem.Value, out duration))
                        {
                            //Duration is in milliseconds.
                            duration /= 1000;
                        }
                    }
                    if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(song) && duration != 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public IEnumerable<ConfigurationSection> GetConfigurationSections()
        {
            return LRCLIBProviderConfiguration.GetConfigurationSections();
        }
    }
}
