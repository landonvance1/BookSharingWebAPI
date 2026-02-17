using System.Diagnostics;
using BookSharingApp.Data;
using BookSharingApp.IntegrationTests.Helpers;
using BookSharingApp.Services;
using BookSharingWebAPI.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.IntegrationTests.Services
{
    /// <summary>
    /// Integration tests for BookCoverAnalysisService using real book cover images and TesseractService.
    ///
    /// Requires the Tesseract CLI to be installed and available in PATH:
    ///   sudo apt-get install tesseract-ocr tesseract-ocr-eng
    ///
    /// Tests return early (pass vacuously) when Tesseract is not available.
    ///
    /// To override the tessdata path for non-standard installs:
    ///   dotnet user-secrets set "Tesseract:TessDataPath" "/path/to/tessdata" --project ../BookSharingApp.csproj
    ///   dotnet user-secrets set "Tesseract:Language"     "eng"               --project ../BookSharingApp.csproj
    ///
    /// To run:
    ///   dotnet test BookSharingApp.IntegrationTests/BookSharingApp.IntegrationTests.csproj
    /// </summary>
    public class TesseractServiceIntegrationTests
    {
        private static readonly string CoverImagesPath = Path.Combine(
            AppContext.BaseDirectory, "TestData", "CoverImages");

        public abstract class TesseractServiceIntegrationTestBase : IDisposable
        {
            // Null when Tesseract is not available — tests return early before use.
            protected readonly BookCoverAnalysisService? Service;
            private readonly ApplicationDbContext _context;
            private readonly bool _tesseractAvailable;

            protected TesseractServiceIntegrationTestBase()
            {
                _tesseractAvailable = IsTesseractCliAvailable();

                var configuration = new ConfigurationBuilder()
                    .AddUserSecrets(typeof(BookCoverAnalysisService).Assembly, optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                var bookLookupService = new OpenLibraryService(
                    new HttpClient(),
                    new Mock<ILogger<OpenLibraryService>>().Object);

                _context = DbContextHelper.CreateInMemoryContext();

                if (_tesseractAvailable)
                {
                    var tesseractService = new TesseractService(
                        configuration,
                        new Mock<ILogger<TesseractService>>().Object);

                    Service = new BookCoverAnalysisService(
                        tesseractService,
                        bookLookupService,
                        _context,
                        new Mock<ILogger<BookCoverAnalysisService>>().Object);
                }
            }

            /// <summary>
            /// Returns true when the Tesseract CLI is not installed — use at the top of each test:
            /// <code>if (TesseractUnavailable) return;</code>
            /// </summary>
            protected bool TesseractUnavailable => !_tesseractAvailable;

            /// <summary>
            /// Opens a stream for a cover image by filename (e.g. "mistborn.jpg").
            /// </summary>
            protected static Stream OpenCoverImage(string filename) =>
                File.OpenRead(Path.Combine(CoverImagesPath, filename));

            public void Dispose()
            {
                _context.Dispose();
                GC.SuppressFinalize(this);
            }

            private static bool IsTesseractCliAvailable()
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "tesseract",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false
                    };
                    psi.ArgumentList.Add("--version");

                    using var proc = Process.Start(psi)!;
                    proc.WaitForExit(5000);
                    return proc.ExitCode == 0;
                }
                catch
                {
                    return false;
                }
            }
        }

        public class AnalyzeCoverAsyncTests : TesseractServiceIntegrationTestBase
        {
            [Fact]
            public async Task AnalyzeCoverAsync_MistbornCover_ReturnsExpectedBook()
            {
                if (TesseractUnavailable) return;
                using var imageStream = OpenCoverImage("mistborn.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-mistborn");

                result.MatchedBooks.Should().Contain(b => b.Title == "Mistborn" && b.Author == "Brandon Sanderson");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_SnowCrashCover_ReturnsExpectedBook()
            {
                if (TesseractUnavailable) return;
                using var imageStream = OpenCoverImage("snow-crash.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-snow-crash");

                result.MatchedBooks.Should().Contain(b => b.Title == "Snow Crash" && b.Author == "Neal Stephenson");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_JadeCityCover_ReturnsExpectedBook()
            {
                if (TesseractUnavailable) return;
                using var imageStream = OpenCoverImage("jade-city.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-jade-city");

                result.MatchedBooks.Should().Contain(b => b.Title == "Jade City" && b.Author == "Fonda Lee");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_GardensOfTheMoonCover_ReturnsExpectedBook()
            {
                if (TesseractUnavailable) return;
                using var imageStream = OpenCoverImage("gardens-of-the-moon.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-gardens");

                result.MatchedBooks.Should().Contain(b => b.Title == "Gardens of the Moon" && b.Author == "Steven Erikson");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_ToKillAMockingbirdCover_ReturnsExpectedBook()
            {
                if (TesseractUnavailable) return;
                using var imageStream = OpenCoverImage("to-kill-a-mockingbird.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-mockingbird");

                result.MatchedBooks.Should().Contain(b => b.Title == "To Kill a Mockingbird" && b.Author == "Harper Lee");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_ARestlessTruthCover_ReturnsExpectedBook()
            {
                if (TesseractUnavailable) return;
                using var imageStream = OpenCoverImage("a-restless-truth.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-restless-truth");

                result.MatchedBooks.Should().Contain(b => b.Title == "A Restless Truth" && b.Author == "Freya Marske");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_UnderTheWhisperingDoorCover_ReturnsExpectedBook()
            {
                if (TesseractUnavailable) return;
                using var imageStream = OpenCoverImage("under-the-whispering-door.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-whispering-door");

                result.MatchedBooks.Should().Contain(b => b.Title == "Under the Whispering Door" && b.Author == "T. J. Klune");
            }
        }
    }
}
