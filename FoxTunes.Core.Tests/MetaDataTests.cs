using NUnit.Framework;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [TestFixture(FILENAME)]
    public class MetaDataTests : TestBase
    {
        public const long FILENAME = 128;

        public MetaDataTests(long configuration)
            : base(configuration)
        {

        }

        public override void SetUp()
        {
            if ((this.Configuration & FILENAME) != 0)
            {
                ComponentResolver.Instance.Add(ComponentSlots.MetaData, "BDAAF3E1-84CC-4D36-A7CB-278663E65844");
            }
            base.SetUp();
        }

        public override void TearDown()
        {
            ComponentResolver.Instance.Remove(ComponentSlots.MetaData);
            base.TearDown();
        }

        [TestCase(@"C:\Music\Delta Heavy\Paradise Lost\01 Paradise Lost.m4a", "Delta Heavy", null, null, "Paradise Lost", null, "01", "Paradise Lost")]
        [TestCase(@"C:\Music\Compilations\Club Anthems 99\1-01 Moloko - Sing It Back.m4a", "Compilations", "Moloko", null, "Club Anthems 99", "1", "01", "Sing It Back")]
        [TestCase(@"C:\Music\Compilations\Club Anthems 99\1-10 Shanks & Bigfoot - Sweet Like Chocolate (Ruff Driverz vocal mix).m4a", "Compilations", "Shanks & Bigfoot", null, "Club Anthems 99", "1", "10", "Sweet Like Chocolate (Ruff Driverz vocal mix)")]
        [TestCase(@"C:\Music\Compilations\Club Anthems 99\2-09 B.B.E. - Seven Days and One Week '99 (Kai Tracid remix).m4a", "Compilations", "B.B.E.", null, "Club Anthems 99", "2", "09", "Seven Days and One Week '99 (Kai Tracid remix)")]
        [TestCase(@"C:\Music\Japan\Akira Yamaoka\2006 - iFUTURELIST\04 Tant pis pour toi.m4a", "Akira Yamaoka", null, "2006", "iFUTURELIST", null, "04", "Tant pis pour toi")]
        public async Task CanReadMetaData(string fileName, string artist, string performer, string year, string album, string disc, string track, string title)
        {
            var metaDataSource = this.Core.Factories.MetaDataSource.Create();
            var metaData = (await metaDataSource.GetMetaData(fileName).ConfigureAwait(false)).ToDictionary(metaDataItem => metaDataItem.Name, metaDataItem => metaDataItem.Value);
            Assert.AreEqual(artist, metaData[CommonMetaData.Artist]);
            if (!string.IsNullOrEmpty(performer))
            {
                Assert.AreEqual(performer, metaData[CommonMetaData.Performer]);
            }
            if (!string.IsNullOrEmpty(year))
            {
                Assert.AreEqual(year, metaData[CommonMetaData.Year]);
            }
            Assert.AreEqual(album, metaData[CommonMetaData.Album]);
            if (!string.IsNullOrEmpty(disc))
            {
                Assert.AreEqual(disc, metaData[CommonMetaData.Disc]);
            }
            Assert.AreEqual(track, metaData[CommonMetaData.Track]);
            Assert.AreEqual(title, metaData[CommonMetaData.Title]);
        }
    }
}
