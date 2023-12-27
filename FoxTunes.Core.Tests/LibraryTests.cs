using FoxDb;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FoxTunes
{
    [TestFixture(SQLITE | TAGLIB)]
    [TestFixture(SQLITE | FILENAME)]
    [TestFixture(SQLSERVER | TAGLIB)]
    [TestFixture(SQLSERVER | FILENAME)]
    public class LibraryTests : DatabaseTests
    {
        public const long TAGLIB = 512;

        public const long FILENAME = 1024;

        public LibraryTests(long configuration)
            : base(configuration)
        {

        }

        public override void SetUp()
        {
            if ((this.Configuration & TAGLIB) != 0)
            {
                ComponentResolver.Instance.Add(ComponentSlots.MetaData, "679D9459-BBCE-4D95-BB65-DD20C335719C");
            }
            else if ((this.Configuration & FILENAME) != 0)
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

        [Test]
        public async Task CanAddFilesToLibrary()
        {
            await this.Core.Managers.Hierarchy.Clear(null, false).ConfigureAwait(false);
            await this.Core.Managers.Library.Clear(null).ConfigureAwait(false);
            await this.Core.Managers.Library.Add(new[]
            {
                TestInfo.AudioFileNames[0],
                TestInfo.AudioFileNames[2],
                TestInfo.AudioFileNames[3]
            }).ConfigureAwait(false);
            this.AssertLibraryItems(
                TestInfo.AudioFileNames[0],
                TestInfo.AudioFileNames[2],
                TestInfo.AudioFileNames[3]
            );
            this.AssertLibraryHierarchy(
                TestInfo.AudioFileNames[0],
                TestInfo.AudioFileNames[2],
                TestInfo.AudioFileNames[3]
            );
            await this.Core.Managers.Library.Clear(null).ConfigureAwait(false);
        }

        [Test]
        public void CanNormalizeLibraryRoots()
        {
            var currentPaths = new[]
            {
                @"C:\a",
                @"C:\1\2\3",
                @"C:\1\2",
                @"C:\1"
            };
            var newPaths = new[]
            {
                @"D:\a",
                @"D:\1\2\3",
                @"D:\1\2",
                @"D:\1"
            };
            var expected = new[]
            {
                @"C:\a",
                @"C:\1",
                @"D:\a",
                @"D:\1"
            };
            var actual = LibraryRoot.Normalize(currentPaths, newPaths).ToArray();
            Assert.AreEqual(expected, actual);
        }

        protected virtual void AssertLibraryItems(params string[] fileNames)
        {
            using (var database = this.Core.Factories.Database.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    var set = database.Set<LibraryItem>(transaction);
                    var query = (
                        from element in set
                        select element
                    ).ToArray();
                    Assert.AreEqual(fileNames.Length, query.Length);
                    for (var a = 0; a < fileNames.Length; a++)
                    {
                        Assert.AreEqual(fileNames[a], query[a].FileName);
                    }
                }
            }
        }

        protected virtual void AssertLibraryHierarchy(params string[] fileNames)
        {
            using (var database = this.Core.Factories.Database.Create())
            {
                using (var transaction = database.BeginTransaction(database.PreferredIsolationLevel))
                {
                    var set = database.Set<LibraryHierarchy>(transaction);
                    foreach (var hierarchy in set)
                    {
                        if (!hierarchy.Enabled)
                        {
                            continue;
                        }
                        var nodes = this.Core.Components.LibraryHierarchyBrowser.GetNodes(hierarchy);
                        this.AssertLibraryHierarchy(hierarchy, nodes, fileNames);
                    }
                }
            }
        }

        private void AssertLibraryHierarchy(LibraryHierarchy hierarchy, IEnumerable<LibraryHierarchyNode> nodes, string[] fileNames)
        {
            var selector = default(Func<LibraryHierarchyNode, IEnumerable<LibraryHierarchyNode>>);
            selector = node =>
            {
                if (node.IsLeaf)
                {
                    return new[] { node };
                }
                node.IsExpanded = true;
                return node.Children.SelectMany(selector);
            };
            var leaves = nodes.SelectMany(selector);
            Assert.AreEqual(fileNames.Count(), leaves.Count());
        }
    }
}
