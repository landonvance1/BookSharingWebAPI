using BookSharingWebAPI.Models;
using BookSharingWebAPI.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class TesseractServiceTests
    {
        public abstract class TesseractServiceTestBase : IDisposable
        {
            protected readonly Mock<ILogger<TesseractService>> LoggerMock;
            protected readonly IConfiguration Configuration;

            protected TesseractServiceTestBase()
            {
                LoggerMock = new Mock<ILogger<TesseractService>>();

                // TessDataPath is optional in the CLI approach — only Language is used at construction
                var configData = new Dictionary<string, string?>
                {
                    { "Tesseract:Language", "eng" }
                };
                Configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(configData)
                    .Build();
            }

            public void Dispose() { }
        }

        public class ConfigurationTests : TesseractServiceTestBase
        {
            [Fact]
            public void Constructor_WithEmptyConfig_DoesNotThrow()
            {
                // Arrange — neither TessDataPath nor Language provided
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string?>())
                    .Build();

                // Act & Assert — both have defaults, nothing is required
                var act = () => new TesseractService(config, LoggerMock.Object);
                act.Should().NotThrow();
            }

            [Fact]
            public void Constructor_WithLanguageOnly_DoesNotThrow()
            {
                // Act & Assert
                var act = () => new TesseractService(Configuration, LoggerMock.Object);
                act.Should().NotThrow();
            }

            [Fact]
            public void Constructor_WithTessDataPathAndLanguage_DoesNotThrow()
            {
                // Arrange
                var configData = new Dictionary<string, string?>
                {
                    { "Tesseract:TessDataPath", "/usr/share/tesseract-ocr/5/tessdata" },
                    { "Tesseract:Language", "eng" }
                };
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(configData)
                    .Build();

                // Act & Assert
                var act = () => new TesseractService(config, LoggerMock.Object);
                act.Should().NotThrow();
            }
        }

        public class TsvParsingTests
        {
            private static string BuildTsvHeader() =>
                "level\tpage_num\tblock_num\tpar_num\tline_num\tword_num\tleft\ttop\twidth\theight\tconf\ttext";

            private static string BuildWordRow(
                int page, int block, int par, int line, int word,
                int left, int top, int width, int height, double conf, string text) =>
                $"5\t{page}\t{block}\t{par}\t{line}\t{word}\t{left}\t{top}\t{width}\t{height}\t{conf:F6}\t{text}";

            [Fact]
            public void ParseTsvOutput_WithSingleLine_ReturnsSingleOcrLine()
            {
                var tsv = string.Join('\n',
                    BuildTsvHeader(),
                    BuildWordRow(1, 1, 1, 1, 1, 10, 20, 200, 50, 96, "Mistborn"),
                    BuildWordRow(1, 1, 1, 1, 2, 220, 20, 100, 50, 92, "Sanderson"));

                var lines = TesseractService.ParseTsvOutput(tsv);

                lines.Should().HaveCount(1);
                lines[0].Text.Should().Be("Mistborn Sanderson");
            }

            [Fact]
            public void ParseTsvOutput_WithMultipleLines_GroupsCorrectly()
            {
                var tsv = string.Join('\n',
                    BuildTsvHeader(),
                    BuildWordRow(1, 1, 1, 1, 1, 10, 20, 300, 60, 96, "The"),
                    BuildWordRow(1, 1, 1, 1, 2, 320, 20, 200, 60, 95, "Hobbit"),
                    BuildWordRow(1, 1, 1, 2, 1, 50, 100, 200, 40, 90, "Tolkien"));

                var lines = TesseractService.ParseTsvOutput(tsv);

                lines.Should().HaveCount(2);
                lines[0].Text.Should().Be("The Hobbit");
                lines[1].Text.Should().Be("Tolkien");
            }

            [Fact]
            public void ParseTsvOutput_ComputesUnionBoundingBox()
            {
                // Two words on the same line: left=10 and left=220+width=320 → union is [10,20,320,70]
                var tsv = string.Join('\n',
                    BuildTsvHeader(),
                    BuildWordRow(1, 1, 1, 1, 1, 10, 20, 200, 50, 96, "Snow"),
                    BuildWordRow(1, 1, 1, 1, 2, 220, 25, 100, 45, 90, "Crash"));

                var lines = TesseractService.ParseTsvOutput(tsv);

                lines.Should().HaveCount(1);
                var bbox = lines[0].BoundingBox!;
                // 8-point polygon: [minLeft, minTop, maxRight, minTop, maxRight, maxBottom, minLeft, maxBottom]
                bbox[0].Should().Be(10);   // minLeft
                bbox[1].Should().Be(20);   // minTop
                bbox[2].Should().Be(320);  // maxRight (220 + 100)
                bbox[5].Should().Be(70);   // maxBottom (25 + 45 = 70, but 20 + 50 = 70 too)
            }

            [Fact]
            public void ParseTsvOutput_SkipsNegativeConfidenceRows()
            {
                var tsv = string.Join('\n',
                    BuildTsvHeader(),
                    // conf=-1 rows are non-word markers (block/line level) — should be skipped
                    "4\t1\t1\t1\t1\t0\t10\t20\t300\t50\t-1\t",
                    BuildWordRow(1, 1, 1, 1, 1, 10, 20, 300, 50, 95, "Dune"));

                var lines = TesseractService.ParseTsvOutput(tsv);

                lines.Should().HaveCount(1);
                lines[0].Text.Should().Be("Dune");
            }

            [Fact]
            public void ParseTsvOutput_SkipsEmptyWordText()
            {
                var tsv = string.Join('\n',
                    BuildTsvHeader(),
                    BuildWordRow(1, 1, 1, 1, 1, 10, 20, 300, 50, 95, "Dune"),
                    BuildWordRow(1, 1, 1, 1, 2, 320, 20, 50, 50, 90, "   ")); // whitespace-only

                var lines = TesseractService.ParseTsvOutput(tsv);

                lines.Should().HaveCount(1);
                lines[0].Text.Should().Be("Dune");
            }

            [Fact]
            public void ParseTsvOutput_WithEmptyInput_ReturnsEmptyList()
            {
                var lines = TesseractService.ParseTsvOutput(BuildTsvHeader());
                lines.Should().BeEmpty();
            }

            [Fact]
            public void ParseTsvOutput_PreservesReadingOrder()
            {
                // Line 2 appears after line 1 in the TSV — output order must match
                var tsv = string.Join('\n',
                    BuildTsvHeader(),
                    BuildWordRow(1, 1, 1, 1, 1, 10, 20, 200, 50, 96, "Title"),
                    BuildWordRow(1, 1, 1, 2, 1, 10, 80, 200, 40, 90, "Author"));

                var lines = TesseractService.ParseTsvOutput(tsv);

                lines.Should().HaveCount(2);
                lines[0].Text.Should().Be("Title");
                lines[1].Text.Should().Be("Author");
            }
        }

        public class BoundingBoxAdaptationTests
        {
            [Fact]
            public void OcrLine_WithAxisAlignedBoundingBox_CalculatesHeightCorrectly()
            {
                // Polygon adapted from Tesseract rect (left=10, top=20, right=110, bottom=70):
                // [10, 20, 110, 20, 110, 70, 10, 70]
                var line = new OcrLine
                {
                    Text = "The Great Gatsby",
                    BoundingBox = new List<double> { 10, 20, 110, 20, 110, 70, 10, 70 }
                };

                line.GetHeight().Should().Be(50);
                line.GetWidth().Should().Be(100);
                line.IsVertical().Should().BeFalse();
                line.GetTextSize().Should().Be(50);
            }

            [Fact]
            public void OcrLine_WithZeroSizedBoundingBox_ReturnsZeroForAllMeasurements()
            {
                var line = new OcrLine
                {
                    Text = "Test",
                    BoundingBox = new List<double> { 10, 10, 10, 10, 10, 10, 10, 10 }
                };

                line.GetHeight().Should().Be(0);
                line.GetWidth().Should().Be(0);
                line.GetTextSize().Should().Be(0);
            }

            [Fact]
            public void OcrTextFilter_WithTesseractAdaptedLines_FiltersSmallText()
            {
                var lines = new List<OcrLine>
                {
                    new() { Text = "Dune",         BoundingBox = new List<double> { 0, 0,   200, 0,   200, 50, 0, 50 } },
                    new() { Text = "Frank Herbert", BoundingBox = new List<double> { 0, 60,  200, 60,  200, 85, 0, 85 } },
                    new() { Text = "tiny promo",    BoundingBox = new List<double> { 0, 90,  200, 90,  200, 100, 0, 100 } }
                };

                var result = OcrTextFilter.ExtractFilteredText(lines);

                result.Should().NotBeEmpty();
                var words = result.Select(w => w.Text).ToList();
                words.Should().Contain("Dune");
                words.Should().Contain("Frank");
                words.Should().Contain("Herbert");
            }

            [Fact]
            public void OcrTextFilter_WithNoBoundingBoxData_FallsBackToBasicFiltering()
            {
                var lines = new List<OcrLine>
                {
                    new() { Text = "Dune" },
                    new() { Text = "Frank Herbert" }
                };

                var result = OcrTextFilter.ExtractFilteredText(lines);

                result.Should().NotBeEmpty();
                var words = result.Select(w => w.Text).ToList();
                words.Should().Contain("Dune");
                words.Should().Contain("Frank");
                words.Should().Contain("Herbert");
            }
        }
    }
}
