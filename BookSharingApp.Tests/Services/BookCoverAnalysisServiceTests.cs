using BookSharingApp.Models;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using BookSharingWebAPI.Models;
using BookSharingWebAPI.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace BookSharingApp.Tests.Services
{
    public class BookCoverAnalysisServiceTests
    {
        public abstract class BookCoverAnalysisServiceTestBase : IDisposable
        {
            protected readonly Mock<IImageAnalysisService> ImageAnalysisServiceMock;
            protected readonly Mock<IBookLookupService> BookLookupServiceMock;
            protected readonly BookCoverAnalysisService Service;
            private readonly BookSharingApp.Data.ApplicationDbContext _context;

            protected BookCoverAnalysisServiceTestBase()
            {
                ImageAnalysisServiceMock = new Mock<IImageAnalysisService>();
                BookLookupServiceMock = new Mock<IBookLookupService>();
                _context = DbContextHelper.CreateInMemoryContext();

                Service = new BookCoverAnalysisService(
                    ImageAnalysisServiceMock.Object,
                    BookLookupServiceMock.Object,
                    _context,
                    new Mock<ILogger<BookCoverAnalysisService>>().Object);
            }

            /// <summary>
            /// Seeds a book into the in-memory database.
            /// </summary>
            protected void SeedBook(int id, string title, string author)
            {
                _context.Books.Add(new Book { Id = id, Title = title, Author = author });
                _context.SaveChanges();
            }

            /// <summary>
            /// Sets up IImageAnalysisService to return a successful OCR result with the given words.
            /// Words are returned without bounding box data (Height = 0) to test the fallback path.
            /// </summary>
            protected void SetupOcrResult(params string[] words)
            {
                var extractedWords = words
                    .Select(w => new ExtractedWord { Text = w, Height = 0 })
                    .ToList();

                ImageAnalysisServiceMock
                    .Setup(s => s.AnalyzeCoverImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CoverAnalysisResult.Success(extractedWords, words.ToList()));
            }

            /// <summary>
            /// Sets up IImageAnalysisService to return a failure result.
            /// </summary>
            protected void SetupOcrFailure(string errorMessage)
            {
                ImageAnalysisServiceMock
                    .Setup(s => s.AnalyzeCoverImageAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(CoverAnalysisResult.Failure(errorMessage));
            }

            public void Dispose() => _context.Dispose();
        }

        public class ExactMatchTests : BookCoverAnalysisServiceTestBase
        {
            [Fact]
            public async Task AnalyzeCoverAsync_WhenBookMatchesAllOcrWords_SetsExactMatch()
            {
                // Arrange — OCR words match the book title and author exactly
                SetupOcrResult("Mistborn", "Brandon", "Sanderson");

                BookLookupServiceMock
                    .Setup(s => s.SearchBooksByTextAsync(It.IsAny<string>()))
                    .ReturnsAsync([new BookLookupResult
                    {
                        Title = "Mistborn",
                        Author = "Brandon Sanderson",
                        ThumbnailUrl = null
                    }]);

                using var stream = new MemoryStream();

                // Act
                var result = await Service.AnalyzeCoverAsync(stream, "image/jpeg", "test");

                // Assert
                result.ExactMatch.Should().NotBeNull();
                result.ExactMatch!.Title.Should().Be("Mistborn");
                result.ExactMatch.Author.Should().Be("Brandon Sanderson");
            }

            [Fact]
            public async Task AnalyzeCoverAsync_WhenOcrIsMissingBookWords_DoesNotSetExactMatch()
            {
                // Arrange — OCR has title + one author word, missing the other.
                // Book words: ["Mistborn", "Brandon", "Sanderson"] = 3
                // Matched: "Mistborn" + "Brandon" = 2 → score = 2/3 ≈ 0.67 (above threshold, but not 100%)
                SetupOcrResult("Mistborn", "Brandon");

                BookLookupServiceMock
                    .Setup(s => s.SearchBooksByTextAsync(It.IsAny<string>()))
                    .ReturnsAsync([new BookLookupResult
                    {
                        Title = "Mistborn",
                        Author = "Brandon Sanderson",
                        ThumbnailUrl = null
                    }]);

                using var stream = new MemoryStream();

                // Act
                var result = await Service.AnalyzeCoverAsync(stream, "image/jpeg", "test");

                // Assert
                result.ExactMatch.Should().BeNull();
                result.MatchedBooks.Should().NotBeEmpty();
            }

            [Fact]
            public async Task AnalyzeCoverAsync_WhenNoMatchesFound_DoesNotSetExactMatch()
            {
                // Arrange
                SetupOcrResult("Mistborn", "Brandon", "Sanderson");

                BookLookupServiceMock
                    .Setup(s => s.SearchBooksByTextAsync(It.IsAny<string>()))
                    .ReturnsAsync([]);

                using var stream = new MemoryStream();

                // Act
                var result = await Service.AnalyzeCoverAsync(stream, "image/jpeg", "test");

                // Assert
                result.ExactMatch.Should().BeNull();
                result.MatchedBooks.Should().BeEmpty();
            }

            [Fact]
            public async Task AnalyzeCoverAsync_WhenLocalBookMatchesAllOcrWords_SetsExactMatch()
            {
                // Arrange — book exists in local DB
                SeedBook(id: 1, title: "Mistborn", author: "Brandon Sanderson");
                SetupOcrResult("Mistborn", "Brandon", "Sanderson");

                BookLookupServiceMock
                    .Setup(s => s.SearchBooksByTextAsync(It.IsAny<string>()))
                    .ReturnsAsync([new BookLookupResult
                    {
                        Title = "Mistborn",
                        Author = "Brandon Sanderson",
                        ThumbnailUrl = null
                    }]);

                using var stream = new MemoryStream();

                // Act
                var result = await Service.AnalyzeCoverAsync(stream, "image/jpeg", "test");

                // Assert
                result.ExactMatch.Should().NotBeNull();
                result.ExactMatch!.Id.Should().Be(1); // Local book preferred
            }

            [Fact]
            public async Task AnalyzeCoverAsync_WhenMultipleBooksReturnedButOneIsExact_SetsExactMatchToHighestScore()
            {
                // Arrange — two books returned, only one is an exact match
                SetupOcrResult("Dune", "Frank", "Herbert");

                BookLookupServiceMock
                    .Setup(s => s.SearchBooksByTextAsync(It.IsAny<string>()))
                    .ReturnsAsync([
                        new BookLookupResult { Title = "Dune", Author = "Frank Herbert" },
                        new BookLookupResult { Title = "Dune Messiah", Author = "Frank Herbert" }
                    ]);

                using var stream = new MemoryStream();

                // Act
                var result = await Service.AnalyzeCoverAsync(stream, "image/jpeg", "test");

                // Assert
                result.ExactMatch.Should().NotBeNull();
                result.ExactMatch!.Title.Should().Be("Dune");
                result.MatchedBooks.Should().HaveCountGreaterThan(1); // Other matches still returned
            }
        }

        public class OcrFailureTests : BookCoverAnalysisServiceTestBase
        {
            [Fact]
            public async Task AnalyzeCoverAsync_WhenOcrFails_ReturnsFailureWithNoExactMatch()
            {
                // Arrange
                SetupOcrFailure("Azure OCR processing failed");

                using var stream = new MemoryStream();

                // Act
                var result = await Service.AnalyzeCoverAsync(stream, "image/jpeg", "test");

                // Assert
                result.Analysis.IsSuccess.Should().BeFalse();
                result.ExactMatch.Should().BeNull();
                result.MatchedBooks.Should().BeEmpty();
            }
        }
    }
}
