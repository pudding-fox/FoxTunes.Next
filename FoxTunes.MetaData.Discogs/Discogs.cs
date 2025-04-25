using FoxTunes.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TinyJson;

namespace FoxTunes
{
    public class Discogs : BaseComponent, IDisposable
    {
        public static RateLimiter RateLimiter { get; private set; }

        static Discogs()
        {
            RateLimiter = new RateLimiter(1000 / MAX_REQUESTS);
        }

        public const string BASE_URL = "https://api.discogs.com";

        public const string KEY = "YhdvfZJhjzLfupJlzesm";

        public const string SECRET = "IwUSieObFFyLInDlctXYmmVNIumjnhiv";

        public const int MAX_REQUESTS = 2;

        public Discogs(string baseUrl = BASE_URL, string key = KEY, string secret = SECRET, int maxRequests = MAX_REQUESTS)
        {
            this.BaseUrl = string.IsNullOrEmpty(baseUrl) ? BASE_URL : baseUrl;
            this.Key = string.IsNullOrEmpty(key) ? KEY : key;
            this.Secret = string.IsNullOrEmpty(secret) ? SECRET : secret;
            if (maxRequests < 0 || maxRequests > 10)
            {
                RateLimiter.Interval = 1000 / MAX_REQUESTS;
            }
            else
            {
                RateLimiter.Interval = 1000 / maxRequests;
            }
        }

        public string BaseUrl { get; private set; }

        public string Key { get; private set; }

        public string Secret { get; private set; }

        public async Task<IEnumerable<Release>> GetReleases(ReleaseLookup releaseLookup, bool master)
        {
            var url = this.GetUrl(releaseLookup, master);
            Logger.Write(this, LogLevel.Debug, "Querying the API: {0}", url);
            var request = this.CreateRequest(url);
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Logger.Write(this, LogLevel.Warn, "Status code does not indicate success.");
                    return Enumerable.Empty<Release>();
                }
                return await this.GetReleases(response.GetResponseStream()).ConfigureAwait(false);
            }
        }

        protected virtual async Task<IEnumerable<Release>> GetReleases(Stream stream)
        {
            const string RESULTS = "results";
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync().ConfigureAwait(false);
                var data = json.FromJson<Dictionary<string, object>>();
                if (data != null)
                {
                    var results = default(object);
                    if (data.TryGetValue(RESULTS, out results) && results is IList<object> resultsList)
                    {
                        return Release.FromResults(resultsList);
                    }
                }
            }
            return Enumerable.Empty<Release>();
        }

        public async Task<ReleaseDetails> GetRelease(Release release)
        {
            var url = release.ResourceUrl;
            Logger.Write(this, LogLevel.Debug, "Querying the API: {0}", url);
            var request = this.CreateRequest(url);
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Logger.Write(this, LogLevel.Warn, "Status code does not indicate success.");
                    return null;
                }
                return await this.GetRelease(response.GetResponseStream()).ConfigureAwait(false);
            }
        }

        protected virtual async Task<ReleaseDetails> GetRelease(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync().ConfigureAwait(false);
                var data = json.FromJson<Dictionary<string, object>>();
                if (data != null)
                {
                    return new ReleaseDetails(data);
                }
            }
            return default(ReleaseDetails);
        }

        public Task<byte[]> GetData(string url)
        {
            Logger.Write(this, LogLevel.Debug, "Querying the API: {0}", url);
            var request = this.CreateRequest(url);
            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Logger.Write(this, LogLevel.Warn, "Status code does not indicate success.");
#if NET40
                    return TaskEx.FromResult(default(byte[]));
#else
                    return Task.FromResult(default(byte[]));
#endif
                }
                return this.GetData(response.GetResponseStream());
            }
        }

        protected virtual Task<byte[]> GetData(Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                using (var temp = new MemoryStream())
                {
                    var buffer = new byte[10240];
                    var count = default(int);
                read:
                    count = reader.Read(buffer, 0, buffer.Length);
                    if (count == 0)
                    {
#if NET40
                        return TaskEx.FromResult(temp.ToArray());
#else
                        return Task.FromResult(temp.ToArray());
#endif
                    }
                    temp.Write(buffer, 0, count);
                    goto read;
                }
            }
        }

        protected virtual string GetUrl(ReleaseLookup releaseLookup, bool master)
        {
            var builder = new StringBuilder();
            builder.Append(this.BaseUrl);
            builder.Append("/database/search?");
            if (master)
            {
                builder.Append("type=master&");
                builder.Append(UrlHelper.GetParameters(new Dictionary<string, string>()
                {
                    { "artist", releaseLookup.Artist },
                    { "release_title", releaseLookup.Album },
                    { "track", releaseLookup.Title }
                }));
            }
            else
            {
                builder.Append("query=");
                builder.Append(this.GetQuery(releaseLookup));
            }
            return builder.ToString();
        }

        protected virtual string GetQuery(ReleaseLookup releaseLookup)
        {
            var builder = new StringBuilder();
            var parts = new[]
            {
                releaseLookup.Artist,
                releaseLookup.Album,
                releaseLookup.Title
            };
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                {
                    continue;
                }
                if (builder.Length > 0)
                {
                    builder.Append(UrlHelper.EscapeDataString(" - "));
                }
                builder.Append(UrlHelper.EscapeDataString(part));
            }
            return builder.ToString();
        }

        protected virtual HttpWebRequest CreateRequest(string url)
        {
            var request = WebRequestFactory.Create(url);
            RateLimiter.Exec(() =>
            {
                request.UserAgent = string.Format("{0}/{1} +{2}", Publication.Product, Publication.Version, Publication.HomePage);
                if (this.Key != null && this.Secret != null)
                {
                    request.Headers[HttpRequestHeader.Authorization] = string.Format("Discogs key={0}, secret={1}", this.Key, this.Secret);
                }
            });
            return request;
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

        ~Discogs()
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

        private static readonly Regex IMAGE_SIZE = new Regex(@"(\d{3,4})x(\d{3,4})");

        public static int ImageSize(string url)
        {
            var match = IMAGE_SIZE.Match(url);
            if (match.Success)
            {
                var a = match.Groups[1].Value;
                var b = match.Groups[2].Value;
                var c = default(int);
                var d = default(int);
                int.TryParse(a, out c);
                int.TryParse(b, out d);
                return c * d;
            }
            else
            {
                return 0;
            }
        }

        private static readonly Regex PARENTHESIZED = new Regex(@"\s*\([^)]*\)\s*", RegexOptions.Compiled);

        public static string Sanitize(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }
            return PARENTHESIZED.Replace(value, string.Empty).Trim();
        }

        public class ReleaseLookup
        {
            const int ERROR_CAPACITY = 10;

            private ReleaseLookup()
            {
                this.Id = Guid.NewGuid();
                this.MetaData = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                this._Errors = new List<string>(ERROR_CAPACITY);
            }

            private ReleaseLookup(IFileData[] fileDatas) : this()
            {
                this.FileDatas = fileDatas;
            }

            public ReleaseLookup(string artist, string title, IFileData[] fileDatas) : this(fileDatas)
            {
                this.Artist = Sanitize(artist);
                this.Title = Sanitize(title);
            }

            public ReleaseLookup(string artist, string album, bool isCompilation, IFileData[] fileDatas) : this(fileDatas)
            {
                this.Artist = Sanitize(artist);
                this.Album = Sanitize(album);
                this.IsCompilation = isCompilation;
            }

            public Guid Id { get; private set; }

            public string Artist { get; private set; }

            public string Album { get; private set; }

            public string Title { get; private set; }

            public bool IsCompilation { get; private set; }

            public ReleaseLookupType Type
            {
                get
                {
                    if (!string.IsNullOrEmpty(this.Artist) && !string.IsNullOrEmpty(this.Album))
                    {
                        return ReleaseLookupType.Album;
                    }
                    else if (!string.IsNullOrEmpty(this.Title))
                    {
                        return ReleaseLookupType.Track;
                    }
                    return ReleaseLookupType.None;
                }
            }

            public IFileData[] FileDatas { get; private set; }

            public Discogs.Release Release { get; set; }

            public IDictionary<string, string> MetaData { get; private set; }

            private IList<string> _Errors { get; set; }

            public IEnumerable<string> Errors
            {
                get
                {
                    return this._Errors;
                }
            }

            public ReleaseLookupStatus Status { get; set; }

            public void AddError(string error)
            {
                this._Errors.Add(error);
                if (this._Errors.Count > ERROR_CAPACITY)
                {
                    this._Errors.RemoveAt(0);
                }
            }

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append(Enum.GetName(typeof(ReleaseLookupType), this.Type));
                builder.Append(": ");
                switch (this.Type)
                {
                    case ReleaseLookupType.Album:
                        builder.AppendFormat("{0} - {1}", this.Artist, this.Album);
                        break;
                    case ReleaseLookupType.Track:
                        if (!string.IsNullOrEmpty(this.Artist))
                        {
                            builder.AppendFormat("{0} - {1}", this.Artist, this.Title);
                        }
                        else
                        {
                            builder.Append(this.Title);
                        }
                        break;
                }
                return builder.ToString();
            }

            public static IEnumerable<ReleaseLookup> FromFileDatas(IEnumerable<IFileData> fileDatas)
            {
                return fileDatas.GroupBy(fileData =>
                {
                    var metaData = default(IDictionary<string, string>);
                    lock (fileData.MetaDatas)
                    {
                        metaData = fileData.MetaDatas.ToDictionary(
                            metaDataItem => metaDataItem.Name,
                            metaDataItem => metaDataItem.Value,
                            StringComparer.OrdinalIgnoreCase
                        );
                    }
                    var artist = metaData.GetValueOrDefault(CommonMetaData.Artist);
                    var album = metaData.GetValueOrDefault(CommonMetaData.Album);
                    var title = metaData.GetValueOrDefault(CommonMetaData.Title);
                    if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(album))
                    {
                        var isCompilation = metaData.Any(new[]
                        {
                            CommonMetaData.IsCompilation,
                            CustomMetaData.VariousArtists
                        }, value => string.Equals(value, bool.TrueString, StringComparison.OrdinalIgnoreCase));
                        if (isCompilation)
                        {
                            artist = Strings.Discogs_CompilationArtist;
                        }
                        return new
                        {
                            Artist = artist,
                            Album = album,
                            Title = default(string),
                            IsCompilation = isCompilation
                        };
                    }
                    else
                    {
                        return new
                        {
                            Artist = artist,
                            Album = default(string),
                            Title = title,
                            IsCompilation = default(bool)
                        };
                    }
                }).Select(group =>
                {
                    if (!string.IsNullOrEmpty(group.Key.Artist) && !string.IsNullOrEmpty(group.Key.Album))
                    {
                        return new ReleaseLookup(group.Key.Artist, group.Key.Album, group.Key.IsCompilation, group.ToArray());
                    }
                    else
                    {
                        return new ReleaseLookup(group.Key.Artist, group.Key.Title, group.ToArray());
                    }
                });
            }
        }

        public enum ReleaseLookupType : byte
        {
            None,
            Album,
            Track
        }

        public enum ReleaseLookupStatus : byte
        {
            None,
            Processing,
            Complete,
            Cancelled,
            Failed
        }

        public class Release
        {
            public static readonly string None = "none (" + Publication.Version + ")";

            private Release(IDictionary<string, object> data)
            {
                const string ID = "id";
                const string URI = "uri";
                const string RESOURCE_URL = "resource_url";
                const string TITLE = "title";
                const string THUMB = "thumb";
                const string COVER_IMAGE = "cover_image";
                this.Id = Convert.ToString(data.GetValueOrDefault(ID));
                this.Url = Convert.ToString(data.GetValueOrDefault(URI));
                this.ResourceUrl = Convert.ToString(data.GetValueOrDefault(RESOURCE_URL));
                this.Title = Convert.ToString(data.GetValueOrDefault(TITLE));
                this.ThumbUrl = Convert.ToString(data.GetValueOrDefault(THUMB));
                this.CoverUrl = Convert.ToString(data.GetValueOrDefault(COVER_IMAGE));
            }

            public string Id { get; private set; }

            public string Url { get; private set; }

            public string ResourceUrl { get; private set; }

            public string Title { get; private set; }

            public string ThumbUrl { get; private set; }

            public int ThumbSize
            {
                get
                {
                    return ImageSize(this.ThumbUrl);
                }
            }

            public string CoverUrl { get; private set; }

            public int CoverSize
            {
                get
                {
                    return ImageSize(this.CoverUrl);
                }
            }

            public float Similarity(ReleaseLookup releaseLookup)
            {
                var title = default(string);
                switch (releaseLookup.Type)
                {
                    case ReleaseLookupType.Album:
                        title = string.Format("{0} - {1}", releaseLookup.Artist, releaseLookup.Album);
                        break;
                    case ReleaseLookupType.Track:
                        if (!string.IsNullOrEmpty(releaseLookup.Artist))
                        {
                            title = string.Format("{0} - {1}", releaseLookup.Artist, releaseLookup.Title);
                        }
                        else
                        {
                            title = releaseLookup.Title;
                        }
                        break;
                }
                var similarity = Sanitize(this.Title).Similarity(title, true);
                return similarity;
            }

            public static IEnumerable<Release> FromResults(IList<object> results)
            {
                if (results != null)
                {
                    foreach (var result in results)
                    {
                        if (result is IDictionary<string, object> data)
                        {
                            yield return new Release(data);
                        }
                    }
                }
            }
        }

        public class ReleaseDetails
        {
            public ReleaseDetails(IDictionary<string, object> data)
            {
                const string ID = "id";
                const string URI = "uri";
                const string RESOURCE_URL = "resource_url";
                const string TITLE = "title";
                const string YEAR = "year";
                const string ARTISTS = "artists";
                const string GENRES = "genres";
                const string TRACKLIST = "tracklist";
                this.Id = Convert.ToString(data.GetValueOrDefault(ID));
                this.Url = Convert.ToString(data.GetValueOrDefault(URI));
                this.ResourceUrl = Convert.ToString(data.GetValueOrDefault(RESOURCE_URL));
                this.Title = Convert.ToString(data.GetValueOrDefault(TITLE));
                this.Year = Convert.ToString(data.GetValueOrDefault(YEAR));
                this.Artists = ArtistDetails.FromResults(data.GetValueOrDefault(ARTISTS) as IList<object>).ToArray();
                this.Genres = data.GetValueOrDefault(GENRES) is IList<object> genres ? genres.OfType<string>().ToArray() : new string[] { };
                this.Tracks = TrackDetails.FromResults(data.GetValueOrDefault(TRACKLIST) as IList<object>).ToArray();
            }

            public string Id { get; private set; }

            public string Url { get; private set; }

            public string ResourceUrl { get; private set; }

            public string Title { get; private set; }

            public string Year { get; private set; }

            public ArtistDetails[] Artists { get; private set; }

            public string[] Genres { get; private set; }

            public TrackDetails[] Tracks { get; private set; }
        }

        public class ArtistDetails
        {
            private ArtistDetails(IDictionary<string, object> data)
            {
                const string ID = "id";
                const string RESOURCE_URL = "resource_url";
                const string THUMBNAIL_URL = "thumbnail_url";
                const string NAME = "name";
                this.Id = Convert.ToString(data.GetValueOrDefault(ID));
                this.ResourceUrl = Convert.ToString(data.GetValueOrDefault(RESOURCE_URL));
                this.ThumbnailUrl = Convert.ToString(data.GetValueOrDefault(THUMBNAIL_URL));
                this.Name = Convert.ToString(data.GetValueOrDefault(NAME));
            }

            public string Id { get; private set; }

            public string ResourceUrl { get; private set; }

            public string ThumbnailUrl { get; private set; }

            public string Name { get; private set; }

            public static IEnumerable<ArtistDetails> FromResults(IList<object> results)
            {
                if (results != null)
                {
                    foreach (var result in results)
                    {
                        if (result is IDictionary<string, object> data)
                        {
                            yield return new ArtistDetails(data);
                        }
                    }
                }
            }
        }

        public class TrackDetails
        {
            private TrackDetails(IDictionary<string, object> data)
            {
                const string POSITION = "position";
                const string TITLE = "title";
                const string EXTRAARTISTS = "extraartists";
                const string TYPE = "type_";
                this.Position = Convert.ToString(data.GetValueOrDefault(POSITION));
                this.Artists = ArtistDetails.FromResults(data.GetValueOrDefault(EXTRAARTISTS) as IList<object>).ToArray();
                this.Title = Convert.ToString(data.GetValueOrDefault(TITLE));
                this.Type = Convert.ToString(data.GetValueOrDefault(TYPE));
            }

            public string Position { get; private set; }

            public ArtistDetails[] Artists { get; private set; }

            public string Title { get; private set; }

            public string Type { get; private set; }

            public static IEnumerable<TrackDetails> FromResults(IList<object> results)
            {
                if (results != null)
                {
                    foreach (var result in results)
                    {
                        if (result is IDictionary<string, object> data)
                        {
                            yield return new TrackDetails(data);
                        }
                    }
                }
            }
        }
    }
}
