using NUnit.Framework;
using System;

namespace FoxTunes
{
    [TestFixture]
    public class FileSystemHelperTests
    {
        [TestCase(@"C:\This\Is\A\Test", @"C:\This\Is\A\Test", @".")]
        [TestCase(@"C:\This\Is\A\Test", @"C:\This\Is", @".\A\Test")]
        [TestCase(@"C:\This\Is", @"C:\This\Is\A\Test", @".\A\Test")]
        public void GetRelativePath(string path1, string path2, string expected)
        {
            var actual = FileSystemHelper.GetRelativePath(path1, path2);
            Assert.AreEqual(expected, actual);
        }

        [TestCase(@"C:\Test\Cats\Dogs", @"C:\Test\Mice")]
        [TestCase(@"C:\Test", @"D:\Test")]
        public void GetRelativePath_NotImplementedException(string path1, string path2)
        {
            Assert.Throws(typeof(NotImplementedException), () =>
            {
                FileSystemHelper.GetRelativePath(path1, path2);
            });
        }
    }
}
