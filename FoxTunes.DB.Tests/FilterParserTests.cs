using FoxTunes.Interfaces;
using NUnit.Framework;
using System;

namespace FoxTunes
{
    [TestFixture]
    public class FilterParserTests : TestBase
    {
        [Test]
        public void FilterParserTest001()
        {
            var filter = "Hello World!";
            var expected = new FilterParserResult(
                new[]
                {
                    new FilterParserResultGroup(
                        new[]
                        {
                            new FilterParserResultEntry(CommonMetaData.Artist, FilterParserEntryOperator.Match, "*Hello World!*"),
                            new FilterParserResultEntry(CommonMetaData.Performer, FilterParserEntryOperator.Match, "*Hello World!*"),
                            new FilterParserResultEntry(CommonMetaData.Album, FilterParserEntryOperator.Match, "*Hello World!*"),
                            new FilterParserResultEntry(CommonMetaData.Title, FilterParserEntryOperator.Match, "*Hello World!*"),
                            new FilterParserResultEntry(FileSystemProperties.FileName, FilterParserEntryOperator.Match, "*Hello World!*"),
                            new FilterParserResultEntry(FileSystemProperties.DirectoryName, FilterParserEntryOperator.Match, "*Hello World!*"),
                        }
                    )
                }
            );
            var actual = default(IFilterParserResult);
            Assert.IsTrue(this.Core.Components.FilterParser.TryParse(
                filter,
                out actual
            ));
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FilterParserTest002()
        {
            var filter = "artist:custard";
            var expected = new FilterParserResult(
                new[]
                {
                    new FilterParserResultGroup(
                        new[]
                        {
                            new FilterParserResultEntry(CommonMetaData.Artist, FilterParserEntryOperator.Match, "*custard*")
                        }
                    )
                }
            );
            var actual = default(IFilterParserResult);
            Assert.IsTrue(this.Core.Components.FilterParser.TryParse(
                filter,
                out actual
            ));
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FilterParserTest003()
        {
            var filter = "artist:custard cream";
            var expected = new FilterParserResult(
                new[]
                {
                    new FilterParserResultGroup(
                        new[]
                        {
                            new FilterParserResultEntry(CommonMetaData.Artist, FilterParserEntryOperator.Match, "*custard cream*")
                        }
                    )
                }
            );
            var actual = default(IFilterParserResult);
            Assert.IsTrue(this.Core.Components.FilterParser.TryParse(
                filter,
                out actual
            ));
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FilterParserTest004()
        {
            var filter = "artist:custard cream year>:1990 year<2000";
            var expected = new FilterParserResult(
                new[]
                {
                    new FilterParserResultGroup(new FilterParserResultEntry(CommonMetaData.Artist, FilterParserEntryOperator.Match, "*custard cream*")),
                    new FilterParserResultGroup(new FilterParserResultEntry(CommonMetaData.Year, FilterParserEntryOperator.GreaterEqual, "1990")),
                    new FilterParserResultGroup(new FilterParserResultEntry(CommonMetaData.Year, FilterParserEntryOperator.Less, "2000")),
                }
            );
            var actual = default(IFilterParserResult);
            Assert.IsTrue(this.Core.Components.FilterParser.TryParse(
                filter,
                out actual
            ));
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FilterParserTest005()
        {
            var filter = "5* cream cakes";
            var expected = new FilterParserResult(
                new[]
                {
                    new FilterParserResultGroup(
                        new[]
                        {
                            new FilterParserResultEntry(CommonStatistics.Rating, FilterParserEntryOperator.Equal, "5"),
                        }
                    ),
                     new FilterParserResultGroup(
                        new[]
                        {
                            new FilterParserResultEntry(CommonMetaData.Artist, FilterParserEntryOperator.Match, "*cream cakes*"),
                            new FilterParserResultEntry(CommonMetaData.Performer, FilterParserEntryOperator.Match,  "*cream cakes*"),
                            new FilterParserResultEntry(CommonMetaData.Album, FilterParserEntryOperator.Match, "*cream cakes*"),
                            new FilterParserResultEntry(CommonMetaData.Title, FilterParserEntryOperator.Match, "*cream cakes*"),
                            new FilterParserResultEntry(FileSystemProperties.FileName, FilterParserEntryOperator.Match, "*cream cakes*"),
                            new FilterParserResultEntry(FileSystemProperties.DirectoryName, FilterParserEntryOperator.Match, "*cream cakes*"),
                        }
                    )
                }
            );
            var actual = default(IFilterParserResult);
            Assert.IsTrue(this.Core.Components.FilterParser.TryParse(
                filter,
                out actual
            ));
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void FilterParserTest006()
        {
            var minRating = "4";
            var maxLastPlayed = DateTimeHelper.ToString(DateTime.Now.AddDays(-30).Date);
            var filter = string.Format("rating>:{0} lastplayed<{1}", minRating, maxLastPlayed);
            var expected = new FilterParserResult(
                new[]
                {
                    new FilterParserResultGroup(
                        new[]
                        {
                            new FilterParserResultEntry(CommonStatistics.Rating, FilterParserEntryOperator.GreaterEqual, "4"),
                        }
                    ),
                    new FilterParserResultGroup(
                        new[]
                        {
                            new FilterParserResultEntry(CommonStatistics.LastPlayed, FilterParserEntryOperator.Less, maxLastPlayed)
                        }
                    )
                }
            );
            var actual = default(IFilterParserResult);
            Assert.IsTrue(this.Core.Components.FilterParser.TryParse(
                filter,
                out actual
            ));
            Assert.AreEqual(expected, actual);
        }
    }
}
