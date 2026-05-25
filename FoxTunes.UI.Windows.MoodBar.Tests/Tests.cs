using NUnit.Framework;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FoxTunes
{
    [TestFixture("..\\Media\\Tracks\\01 Balrog Boogie.mp3", 789111553)]
    [TestFixture("..\\Media\\Tracks\\02 Heroines.mp3", 1151439372)]
    [TestFixture("..\\Media\\Tracks\\03 Poetic Pitbull Revolutions.mp3", 1727178400)]
    public class Tests : TestBase
    {
        public static string Location
        {
            get
            {
                return Path.GetDirectoryName(typeof(Tests).Assembly.Location);
            }
        }

        public Tests(string fileName, int hashCode)
        {
            this.FileName = Path.Combine(Path.GetDirectoryName(Location), fileName);
            this.HashCode = hashCode;
        }

        public string FileName { get; private set; }

        public int HashCode { get; private set; }

        [Test]
        public async Task Test001()
        {
            var moodBarItems = new[]
            {
                MoodBarItem.FromFileData(new PlaylistItem()
                {
                    FileName = this.FileName
                })
            };
            using (var moodBar = new MoodBar(moodBarItems))
            {
                moodBar.InitializeComponent(this.Core);
                await moodBar.Create().ConfigureAwait(false);
                Assert.That(this.HashCode == this.GetHashCode(moodBarItems[0]));
            }
        }

        private int GetHashCode(MoodBarItem moodBarItem)
        {
            var hashCode = 0;
            unchecked
            {
                for (var a = 0; a < moodBarItem.Data.Data.GetLength(0); a++)
                {
                    for (var b = 0; b < moodBarItem.Data.Data.GetLength(1); b++)
                    {
                        hashCode = (hashCode * 397) ^ moodBarItem.Data.Data[a, b].GetHashCode();
                    }
                }
            }
            return Math.Abs(hashCode);
        }
    }
}
