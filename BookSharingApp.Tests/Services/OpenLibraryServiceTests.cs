using BookSharingApp.Common;
using BookSharingApp.Services;
using BookSharingApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace BookSharingApp.Tests.Services
{
    public class OpenLibraryServiceTests
    {
        // Shared base class for all nested test classes
        public abstract class OpenLibraryServiceTestBase : IDisposable
        {
            protected readonly Mock<ILogger<OpenLibraryService>> LoggerMock;

            protected OpenLibraryServiceTestBase()
            {
                LoggerMock = new Mock<ILogger<OpenLibraryService>>();
            }

            public void Dispose()
            {
                // Cleanup if needed
            }
        }

        public class GetBookByIsbnAsyncTests : OpenLibraryServiceTestBase
        {
            [Fact]
            public async Task GetBookByIsbnAsync_WithCompleteBookData_ReturnsBookLookupResult()
            {
                // Arrange
                var isbn = "9780140328721";
                var jsonResponse = @"{
                    ""num_found"": 1,
                    ""docs"": [
                        {
                            ""title"": ""Fantastic Mr. Fox"",
                            ""author_name"": [""Roald Dahl""],
                            ""isbn"": [""9780140328721""],
                            ""cover_i"": 8739161
                        }
                    ]
                }";

                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler(jsonResponse);
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var result = await service.GetBookByIsbnAsync(isbn);

                // Assert
                result.Should().NotBeNull();
                result!.Title.Should().Be("Fantastic Mr. Fox");
                result.Author.Should().Be("Roald Dahl");
                result.Isbn.Should().Be(isbn);
                result.ThumbnailUrl.Should().Be("https://covers.openlibrary.org/b/id/8739161-M.jpg");
            }

            [Fact]
            public async Task GetBookByIsbnAsync_WithMissingOptionalFields_ReturnsBookWithDefaults()
            {
                // Arrange
                var isbn = "1234567890";
                var jsonResponse = @"{
                    ""num_found"": 1,
                    ""docs"": [
                        {
                            ""title"": ""Test Book""
                        }
                    ]
                }";

                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler(jsonResponse);
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var result = await service.GetBookByIsbnAsync(isbn);

                // Assert
                result.Should().NotBeNull();
                result!.Title.Should().Be("Test Book");
                result.Author.Should().Be("Unknown Author");
                result.Isbn.Should().Be(isbn);
                result.ThumbnailUrl.Should().BeNull();
            }

            [Fact]
            public async Task GetBookByIsbnAsync_WithDashesAndSpaces_CleansIsbn()
            {
                // Arrange
                var isbnWithFormatting = "978-0-14-032872-1";
                var cleanIsbn = "9780140328721";
                var jsonResponse = @"{
                    ""num_found"": 1,
                    ""docs"": [
                        {
                            ""title"": ""Test Book"",
                            ""author_name"": [""Test Author""]
                        }
                    ]
                }";

                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler(jsonResponse);
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var result = await service.GetBookByIsbnAsync(isbnWithFormatting);

                // Assert
                result.Should().NotBeNull();
                result!.Isbn.Should().Be(cleanIsbn);
            }

            [Fact]
            public async Task GetBookByIsbnAsync_WhenNoBooksFound_ReturnsNull()
            {
                // Arrange
                var isbn = "0000000000";
                var jsonResponse = @"{
                    ""num_found"": 0,
                    ""docs"": []
                }";

                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler(jsonResponse);
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var result = await service.GetBookByIsbnAsync(isbn);

                // Assert
                result.Should().BeNull();
            }

            [Fact]
            public async Task GetBookByIsbnAsync_WhenApiReturnsError_ReturnsNull()
            {
                // Arrange
                var isbn = "1234567890";
                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler("", HttpStatusCode.InternalServerError);
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var result = await service.GetBookByIsbnAsync(isbn);

                // Assert
                result.Should().BeNull();
            }

            [Fact]
            public async Task GetBookByIsbnAsync_WhenNetworkExceptionOccurs_ReturnsNull()
            {
                // Arrange
                var isbn = "1234567890";
                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandlerWithException(
                    new HttpRequestException("Network error"));
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var result = await service.GetBookByIsbnAsync(isbn);

                // Assert
                result.Should().BeNull();
            }

            [Fact]
            public async Task GetBookByIsbnAsync_WithNullTitleInResponse_UsesUnknownTitle()
            {
                // Arrange
                var isbn = "1234567890";
                var jsonResponse = @"{
                    ""num_found"": 1,
                    ""docs"": [
                        {
                            ""title"": null,
                            ""author_name"": [""Test Author""]
                        }
                    ]
                }";

                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler(jsonResponse);
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var result = await service.GetBookByIsbnAsync(isbn);

                // Assert
                result.Should().NotBeNull();
                result!.Title.Should().Be("Unknown Title");
            }
        }

        public class SearchBooksAsyncTests : OpenLibraryServiceTestBase
        {
            [Fact]
            public async Task SearchBooksAsync_WithTitleOnly_ReturnsBooks()
            {
                // Arrange
                var title = "Dune";
                var jsonResponse = @"{
                    ""num_found"": 2,
                    ""docs"": [
                        {
                            ""title"": ""Dune"",
                            ""author_name"": [""Frank Herbert""],
                            ""isbn"": [""9780441172719""],
                            ""cover_i"": 258027
                        },
                        {
                            ""title"": ""Dune Messiah"",
                            ""author_name"": [""Frank Herbert""],
                            ""isbn"": [""9780441172696""]
                        }
                    ]
                }";

                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler(jsonResponse);
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var results = await service.SearchBooksAsync(title: title);

                // Assert
                results.Should().HaveCount(2);
                results[0].Title.Should().Be("Dune");
                results[0].Author.Should().Be("Frank Herbert");
                results[0].Isbn.Should().Be("9780441172719");
                results[0].ThumbnailUrl.Should().Be("https://covers.openlibrary.org/b/id/258027-M.jpg");
                results[1].Title.Should().Be("Dune Messiah");
                results[1].ThumbnailUrl.Should().BeNull();
            }

            [Fact]
            public async Task SearchBooksAsync_WithMultipleParameters_ReturnsBooks()
            {
                // Arrange
                var isbn = "9780441172719";
                var title = "Dune";
                var author = "Herbert";
                var jsonResponse = @"{
                    ""num_found"": 1,
                    ""docs"": [
                        {
                            ""title"": ""Dune"",
                            ""author_name"": [""Frank Herbert""],
                            ""isbn"": [""9780441172719""],
                            ""cover_i"": 258027
                        }
                    ]
                }";

                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler(jsonResponse);
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var results = await service.SearchBooksAsync(isbn: isbn, title: title, author: author);

                // Assert
                results.Should().HaveCount(1);
                results[0].Title.Should().Be("Dune");
                results[0].Author.Should().Be("Frank Herbert");
                results[0].Isbn.Should().Be(isbn);
            }

            [Fact]
            public async Task SearchBooksAsync_WithNoParameters_ReturnsEmptyList()
            {
                // Arrange
                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler("{}");
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var results = await service.SearchBooksAsync();

                // Assert
                results.Should().BeEmpty();
            }

            [Fact]
            public async Task SearchBooksAsync_WhenNoBooksFound_ReturnsEmptyList()
            {
                // Arrange
                var title = "NonexistentBook12345";
                var jsonResponse = @"{
                    ""num_found"": 0,
                    ""docs"": []
                }";

                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler(jsonResponse);
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var results = await service.SearchBooksAsync(title: title);

                // Assert
                results.Should().BeEmpty();
            }

            [Fact]
            public async Task SearchBooksAsync_WhenApiReturnsError_ReturnsEmptyList()
            {
                // Arrange
                var title = "Test";
                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler("", HttpStatusCode.BadRequest);
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var results = await service.SearchBooksAsync(title: title);

                // Assert
                results.Should().BeEmpty();
            }

            [Fact]
            public async Task SearchBooksAsync_WhenNetworkExceptionOccurs_ReturnsEmptyList()
            {
                // Arrange
                var title = "Test";
                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandlerWithException(
                    new TaskCanceledException("Request timeout"));
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var results = await service.SearchBooksAsync(title: title);

                // Assert
                results.Should().BeEmpty();
            }

            [Fact]
            public async Task SearchBooksAsync_IncludesLimitParameter()
            {
                // Arrange
                var title = "Test";
                var expectedLimit = Constants.MaxExternalSearchResults + 1; // Should be 21
                var jsonResponse = @"{
                    ""num_found"": 0,
                    ""docs"": []
                }";

                HttpRequestMessage? capturedRequest = null;
                var mockHandler = new Mock<System.Net.Http.HttpMessageHandler>();
                mockHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.IsAny<HttpRequestMessage>(),
                        ItExpr.IsAny<CancellationToken>()
                    )
                    .Callback<HttpRequestMessage, CancellationToken>((req, token) => capturedRequest = req)
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(jsonResponse)
                    });

                var httpClient = new HttpClient(mockHandler.Object);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                await service.SearchBooksAsync(title: title);

                // Assert
                capturedRequest.Should().NotBeNull();
                capturedRequest!.RequestUri.Should().NotBeNull();
                capturedRequest.RequestUri!.Query.Should().Contain($"limit={expectedLimit}");
            }

            [Fact]
            public async Task SearchBooksAsync_WithMissingOptionalFields_UsesDefaults()
            {
                // Arrange
                var title = "Test";
                var jsonResponse = @"{
                    ""num_found"": 2,
                    ""docs"": [
                        {
                            ""title"": ""Book with Author""
                        },
                        {
                            ""title"": null,
                            ""author_name"": [""Some Author""],
                            ""isbn"": [""1234567890""]
                        }
                    ]
                }";

                var mockHandler = MockHttpMessageHandlerHelper.CreateMockHandler(jsonResponse);
                var httpClient = new HttpClient(mockHandler);
                var service = new OpenLibraryService(httpClient, LoggerMock.Object);

                // Act
                var results = await service.SearchBooksAsync(title: title);

                // Assert
                results.Should().HaveCount(2);
                results[0].Title.Should().Be("Book with Author");
                results[0].Author.Should().Be("Unknown Author");
                results[0].Isbn.Should().BeEmpty();
                results[0].ThumbnailUrl.Should().BeNull();

                results[1].Title.Should().Be("Unknown Title");
                results[1].Author.Should().Be("Some Author");
                results[1].Isbn.Should().Be("1234567890");
            }
        }
    }
}
