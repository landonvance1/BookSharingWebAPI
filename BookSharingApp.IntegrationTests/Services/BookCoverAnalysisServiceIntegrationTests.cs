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
    /// Integration tests for BookCoverAnalysisService using real book cover images.
    ///
    /// Calls the real AzureVisionService and OpenLibraryService. Requires valid Azure
    /// credentials — tests return early (pass vacuously) when credentials are absent.
    ///
    /// To configure credentials:
    ///   dotnet user-secrets set "AzureVision:Endpoint" "https://..." --project ../BookSharingApp.csproj
    ///   dotnet user-secrets set "AzureVision:ApiKey"   "..."          --project ../BookSharingApp.csproj
    ///
    /// To run:
    ///   dotnet test BookSharingApp.IntegrationTests/BookSharingApp.IntegrationTests.csproj
    /// </summary>
    public class BookCoverAnalysisServiceIntegrationTests
    {
        private static readonly string CoverImagesPath = Path.Combine(
            AppContext.BaseDirectory, "TestData", "CoverImages");

        public abstract class BookCoverAnalysisServiceIntegrationTestBase : IDisposable
        {
            // Null when credentials are not configured — tests return early before use.
            protected readonly BookCoverAnalysisService? Service;
            private readonly ApplicationDbContext _context;
            private readonly bool _credentialsConfigured;

            protected BookCoverAnalysisServiceIntegrationTestBase()
            {
                var configuration = new ConfigurationBuilder()
                    .AddUserSecrets(typeof(BookCoverAnalysisService).Assembly, optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                var endpoint = configuration["AzureVision:Endpoint"];
                var apiKey = configuration["AzureVision:ApiKey"];
                _credentialsConfigured = !string.IsNullOrWhiteSpace(endpoint) &&
                                         !string.IsNullOrWhiteSpace(apiKey);

                var bookLookupService = new OpenLibraryService(
                    new HttpClient(),
                    new Mock<ILogger<OpenLibraryService>>().Object);

                _context = DbContextHelper.CreateInMemoryContext();

                if (_credentialsConfigured)
                {
                    var visionService = new AzureVisionService(
                        new HttpClient(),
                        configuration,
                        new Mock<ILogger<AzureVisionService>>().Object);

                    Service = new BookCoverAnalysisService(
                        visionService,
                        bookLookupService,
                        _context,
                        new Mock<ILogger<BookCoverAnalysisService>>().Object);
                }
            }

            /// <summary>
            /// Returns true when Azure credentials are absent — use at the top of each test to skip:
            /// <code>if (CredentialsMissing) return;</code>
            /// </summary>
            protected bool CredentialsMissing => !_credentialsConfigured;

            /// <summary>
            /// Opens a stream for a cover image by filename (e.g. "mistborn.jpg").
            /// </summary>
            protected static Stream OpenCoverImage(string filename) =>
                File.OpenRead(Path.Combine(CoverImagesPath, filename));

            public void Dispose() => _context.Dispose();
        }

        public class AnalyzeCoverAsyncTests : BookCoverAnalysisServiceIntegrationTestBase
        {
            [Fact]
            public async Task AnalyzeCoverAsync_MistbornCover_ReturnsExpectedBook()
            {
                using var imageStream = OpenCoverImage("mistborn.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-mistborn");

                result.MatchedBooks.Should().Contain(b => b.Title == "Mistborn" && b.Author == "Brandon Sanderson");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_SnowCrashCover_ReturnsExpectedBook()
            {
                using var imageStream = OpenCoverImage("snow-crash.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-snow-crash");
                
                result.MatchedBooks.Should().Contain(b => b.Title == "Snow Crash" && b.Author == "Neal Stephenson");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_JadeCityCover_ReturnsExpectedBook()
            {
                using var imageStream = OpenCoverImage("jade-city.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-jade-city");

                result.MatchedBooks.Should().Contain(b => b.Title == "Jade City" && b.Author == "Fonda Lee");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_GardensOfTheMoonCover_ReturnsExpectedBook()
            {
                using var imageStream = OpenCoverImage("gardens-of-the-moon.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-gardens");

                result.MatchedBooks.Should().Contain(b => b.Title == "Gardens of the Moon" && b.Author == "Steven Erikson");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_ToKillAMockingbirdCover_ReturnsExpectedBook()
            {
                using var imageStream = OpenCoverImage("to-kill-a-mockingbird.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-mockingbird");

                result.MatchedBooks.Should().Contain(b => b.Title == "To Kill a Mockingbird" && b.Author == "Harper Lee");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_ARestlessTruthCover_ReturnsExpectedBook()
            {
                using var imageStream = OpenCoverImage("a-restless-truth.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-restless-truth");

                result.MatchedBooks.Should().Contain(b => b.Title == "A Restless Truth" && b.Author == "Freya Marske");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_UnderTheWhisperingDoorCover_ReturnsExpectedBook()
            {
                using var imageStream = OpenCoverImage("under-the-whispering-door.jpg");

                var result = await Service!.AnalyzeCoverAsync(imageStream, "image/jpeg", "test-whispering-door");

                result.MatchedBooks.Should().Contain(b => b.Title == "Under the Whispering Door" && b.Author == "T. J. Klune");
            }
        }
    }
}
