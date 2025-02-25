using FoxTunes.Interfaces;
using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes.MetaData.Discogs.Tests
{
    [TestFixture]
    public class DiscogsTests : TestBase
    {
        [TestCase("nirvana", "nevermind", "")]
        [TestCase("nirvana", "", "smells like teen spirit")]
        public async Task CanGetReleases(string artist, string album, string title)
        {
            var discogs = new global::FoxTunes.Discogs();
            discogs.InitializeComponent(this.Core);
            var releaseLookup = default(global::FoxTunes.Discogs.ReleaseLookup);
            if (!string.IsNullOrEmpty(artist) && !string.IsNullOrEmpty(album))
            {
                releaseLookup = new global::FoxTunes.Discogs.ReleaseLookup(artist, album, false, new IFileData[] { });
            }
            else
            {
                releaseLookup = new global::FoxTunes.Discogs.ReleaseLookup(artist, title, new IFileData[] { });
            }
            var releases = await discogs.GetReleases(releaseLookup, true).ConfigureAwait(false);
            Assert.GreaterOrEqual(releases.ToArray().Length, 1);
        }

        [TestCase("https://img.discogs.com/yHidVUUKaW6hnmCxhmUDY1hwnHs=/fit-in/320x317/filters:strip_icc():format(jpeg):mode_rgb():quality(90)/discogs-images/R-3686829-1340531254-3543.jpeg.jpg")]
        public async Task CanGetData(string url)
        {
            var discogs = new global::FoxTunes.Discogs();
            discogs.InitializeComponent(this.Core);
            var data = await discogs.GetData(url).ConfigureAwait(false);
            Assert.GreaterOrEqual(data.Length, 1);
        }
    }
}
