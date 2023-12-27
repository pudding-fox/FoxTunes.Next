using FoxTunes.Interfaces;
using FoxTunes.Templates;
using NUnit.Framework;

namespace FoxTunes
{
    [TestFixture]
    public class PlaylistSortBuilderTests : TestBase
    {
        [Test]
        public void PlaylistSortBuilderTest001()
        {
            var sort = new SortParserResult(
                new[]
                {
                    new SortParserResultExpression(
                        CustomMetaData.VariousArtists,
                        SortParserResultOperator.NullCoalesce,
                        new SortParserResultExpression(
                            CommonMetaData.Artist
                        )
                    ),
                    new SortParserResultExpression(
                        CommonMetaData.Year
                    ),
                    new SortParserResultExpression(
                        CommonMetaData.Album
                    ),
                    new SortParserResultExpression(
                        CommonMetaData.Disc,
                        SortParserResultOperator.Numeric
                    ),
                    new SortParserResultExpression(
                        CommonMetaData.Track,
                        SortParserResultOperator.Numeric
                    )
                }
            );
            var expected = @"
CASE
	WHEN ""HorizontalMetaData"".""Value_0_Value"" IS NOT NULL 
		THEN ""HorizontalMetaData"".""Value_0_Value""
	ELSE ""HorizontalMetaData"".""Value_1_Value""
END, ""HorizontalMetaData"".""Value_2_Value"", ""HorizontalMetaData"".""Value_3_Value"", CAST(""HorizontalMetaData"".""Value_4_Value"" AS int), CAST(""HorizontalMetaData"".""Value_5_Value"" AS int), ""HorizontalMetaData"".""FileName""";
            var actual = default(string);
            using (var database = this.Core.Factories.Database.Create())
            {
                var builder = new PlaylistSortBuilder(database, sort);
                actual = builder.TransformText();
            }
            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void PlaylistSortBuilderTest002()
        {
            var sort = new SortParserResult(
                new[]
                {
                    new SortParserResultExpression(
                        CommonMetaData.IsCompilation,
                        SortParserResultOperator.NullCoalesce,
                        new SortParserResultExpression(
                            CustomMetaData.VariousArtists,
                            SortParserResultOperator.NullCoalesce,
                            new SortParserResultExpression(
                                CommonMetaData.Artist
                            )
                        )
                    ),
                    new SortParserResultExpression(
                        CommonMetaData.Year
                    ),
                    new SortParserResultExpression(
                        CommonMetaData.Album
                    ),
                    new SortParserResultExpression(
                        CommonMetaData.Disc,
                        SortParserResultOperator.Numeric
                    ),
                    new SortParserResultExpression(
                        CommonMetaData.Track,
                        SortParserResultOperator.Numeric
                    )
                }
            );
            var expected = @"
CASE
	WHEN ""HorizontalMetaData"".""Value_0_Value"" IS NOT NULL 
		THEN ""HorizontalMetaData"".""Value_0_Value""
	WHEN ""HorizontalMetaData"".""Value_1_Value"" IS NOT NULL 
		THEN ""HorizontalMetaData"".""Value_1_Value""
	ELSE ""HorizontalMetaData"".""Value_2_Value""
END, ""HorizontalMetaData"".""Value_3_Value"", ""HorizontalMetaData"".""Value_4_Value"", CAST(""HorizontalMetaData"".""Value_5_Value"" AS int), CAST(""HorizontalMetaData"".""Value_6_Value"" AS int), ""HorizontalMetaData"".""FileName""";
            var actual = default(string);
            using (var database = this.Core.Factories.Database.Create())
            {
                var builder = new PlaylistSortBuilder(database, sort);
                actual = builder.TransformText();
            }
            Assert.AreEqual(expected, actual);
        }
    }
}
