using NUnit.Framework;
using System;
using System.Linq;

namespace FoxTunes
{
    [TestFixture]
    public class PLSHelperTests
    {
        [Test]
        public void Reader_Read_1()
        {
            var playlist = Resources.Playlist1.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            var reader = new PLSHelper.Reader(playlist);
            var playlistItems = reader.Read().ToArray();
            Assert.AreEqual(4, playlistItems.Length);
        }
    }
}
