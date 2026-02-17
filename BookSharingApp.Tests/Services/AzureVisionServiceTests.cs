using BookSharingWebAPI.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace BookSharingApp.Tests.Services
{
    public class AzureVisionServiceTests
    {
        // Shared base class for all nested test classes
        public abstract class AzureVisionServiceTestBase : IDisposable
        {
            protected readonly Mock<ILogger<AzureVisionService>> LoggerMock;
            protected readonly IConfiguration Configuration;

            protected AzureVisionServiceTestBase()
            {
                LoggerMock = new Mock<ILogger<AzureVisionService>>();

                var configData = new Dictionary<string, string?>
                {
                    { "AzureVision:Endpoint", "https://test.cognitiveservices.azure.com" },
                    { "AzureVision:ApiKey", "test-api-key" }
                };
                Configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(configData)
                    .Build();
            }

            /// <summary>
            /// Creates a mock HttpMessageHandler that simulates Azure Vision's two-step OCR process
            /// </summary>
            protected HttpMessageHandler CreateAzureMockHandler(
                List<string> ocrLines,
                string operationStatus = "succeeded")
            {
                var operationUrl = "https://test.cognitiveservices.azure.com/vision/v3.2/read/operations/123";

                var mockHandler = new Mock<HttpMessageHandler>();

                // Setup for POST request (initiate OCR)
                mockHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req =>
                            req.Method == HttpMethod.Post &&
                            req.RequestUri!.PathAndQuery.Contains("/read/analyze")),
                        ItExpr.IsAny<CancellationToken>()
                    )
                    .ReturnsAsync(() =>
                    {
                        var response = new HttpResponseMessage(HttpStatusCode.Accepted);
                        response.Headers.Add("Operation-Location", operationUrl);
                        return response;
                    });

                // Setup for GET request (poll for results)
                var lines = ocrLines.Select(text => new { text }).ToList();
                var resultJson = $$"""
                {
                    "status": "{{operationStatus}}",
                    "analyzeResult": {
                        "readResults": [
                            {
                                "lines": {{System.Text.Json.JsonSerializer.Serialize(lines)}}
                            }
                        ]
                    }
                }
                """;

                mockHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req =>
                            req.Method == HttpMethod.Get &&
                            req.RequestUri!.ToString() == operationUrl),
                        ItExpr.IsAny<CancellationToken>()
                    )
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(resultJson)
                    });

                return mockHandler.Object;
            }

            /// <summary>
            /// Creates a valid JPEG image byte array (minimal valid JPEG header)
            /// </summary>
            protected byte[] CreateValidJpegImage()
            {
                // Minimal valid JPEG: FFD8 (start) + FFD9 (end)
                return new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
            }

            public void Dispose()
            {
                // Cleanup if needed
            }
        }

        public class AnalyzeCoverImageAsyncNoBoundingBoxFallbackTests : AzureVisionServiceTestBase
        {
            [Fact]
            public async Task AnalyzeCoverImageAsync_WithNoBoundingBoxData_FallsBackToBasicFilteringAndReturnsSuccess()
            {
                // Arrange
                var ocrLines = new List<string>
                {
                    "THE GREAT GATSBY",
                    "by F. Scott Fitzgerald"
                };

                var mockHandler = CreateAzureMockHandler(ocrLines);
                var httpClient = new HttpClient(mockHandler);
                var service = new AzureVisionService(httpClient, Configuration, LoggerMock.Object, pollingDelayMs: 0);

                var imageBytes = CreateValidJpegImage();
                using var stream = new MemoryStream(imageBytes);

                // Act
                var result = await service.AnalyzeCoverImageAsync(stream, "image/jpeg");

                // Assert
                result.IsSuccess.Should().BeTrue();
                result.RawExtractedText.Should().HaveCount(2);
                result.FilteredText.Should().NotBeEmpty();
                result.FilteredText.Should().AllSatisfy(w => w.Text.Should().NotBeNullOrWhiteSpace());
            }

            [Fact]
            public async Task AnalyzeCoverImageAsync_WithNoBoundingBoxData_ExtractsTitleAndAuthorWords()
            {
                // Arrange
                var ocrLines = new List<string>
                {
                    "1984",
                    "George Orwell"
                };

                var mockHandler = CreateAzureMockHandler(ocrLines);
                var httpClient = new HttpClient(mockHandler);
                var service = new AzureVisionService(httpClient, Configuration, LoggerMock.Object, pollingDelayMs: 0);

                var imageBytes = CreateValidJpegImage();
                using var stream = new MemoryStream(imageBytes);

                // Act
                var result = await service.AnalyzeCoverImageAsync(stream, "image/jpeg");

                // Assert
                result.IsSuccess.Should().BeTrue();
                result.FilteredText.Should().NotBeEmpty();
                result.RawExtractedText.Should().HaveCount(2);
            }

            [Fact]
            public async Task AnalyzeCoverImageAsync_WithNoBoundingBoxData_ExtractsWordsFromMultipleLines()
            {
                // Arrange
                var ocrLines = new List<string>
                {
                    "To Kill a Mockingbird",
                    "by Harper Lee"
                };

                var mockHandler = CreateAzureMockHandler(ocrLines);
                var httpClient = new HttpClient(mockHandler);
                var service = new AzureVisionService(httpClient, Configuration, LoggerMock.Object, pollingDelayMs: 0);

                var imageBytes = CreateValidJpegImage();
                using var stream = new MemoryStream(imageBytes);

                // Act
                var result = await service.AnalyzeCoverImageAsync(stream, "image/jpeg");

                // Assert
                result.IsSuccess.Should().BeTrue();
                result.FilteredText.Should().NotBeEmpty();
                var words = result.FilteredText.Select(w => w.Text).ToList();
                words.Should().Contain("Mockingbird");
                words.Should().Contain(w => w.Contains("Harper") || w.Contains("Lee"));
            }

            [Fact]
            public async Task AnalyzeCoverImageAsync_WhenAzureProcessingFails_ThrowsInvalidOperationException()
            {
                // Arrange
                var mockHandler = CreateAzureMockHandler(new List<string>(), operationStatus: "failed");
                var httpClient = new HttpClient(mockHandler);
                var service = new AzureVisionService(httpClient, Configuration, LoggerMock.Object, pollingDelayMs: 0);

                var imageBytes = CreateValidJpegImage();
                using var stream = new MemoryStream(imageBytes);

                // Act & Assert
                var act = () => service.AnalyzeCoverImageAsync(stream, "image/jpeg");
                await act.Should().ThrowAsync<InvalidOperationException>()
                    .WithMessage("*Azure OCR processing failed*");
            }

            [Fact]
            public async Task AnalyzeCoverImageAsync_WithNoBoundingBoxAndShortWords_ReturnsEmptyFilteredText()
            {
                // Arrange
                var ocrLines = new List<string>
                {
                    "12", // Too short
                    "AB"  // Too short
                };

                var mockHandler = CreateAzureMockHandler(ocrLines);
                var httpClient = new HttpClient(mockHandler);
                var service = new AzureVisionService(httpClient, Configuration, LoggerMock.Object, pollingDelayMs: 0);

                var imageBytes = CreateValidJpegImage();
                using var stream = new MemoryStream(imageBytes);

                // Act
                var result = await service.AnalyzeCoverImageAsync(stream, "image/jpeg");

                // Assert
                result.IsSuccess.Should().BeTrue();
                result.FilteredText.Should().BeEmpty(); // Text too short to filter
            }
        }

        public class WordCountLimitingTests : AzureVisionServiceTestBase
        {
            /// <summary>
            /// Creates a mock handler that returns OCR lines with bounding box data
            /// </summary>
            private HttpMessageHandler CreateMockHandlerWithBoundingBoxes(
                List<(string text, double height)> linesWithHeights,
                string operationStatus = "succeeded")
            {
                var operationUrl = "https://test.cognitiveservices.azure.com/vision/v3.2/read/operations/123";
                var mockHandler = new Mock<HttpMessageHandler>();

                // Setup POST request
                mockHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req =>
                            req.Method == HttpMethod.Post &&
                            req.RequestUri!.PathAndQuery.Contains("/read/analyze")),
                        ItExpr.IsAny<CancellationToken>()
                    )
                    .ReturnsAsync(() =>
                    {
                        var response = new HttpResponseMessage(HttpStatusCode.Accepted);
                        response.Headers.Add("Operation-Location", operationUrl);
                        return response;
                    });

                // Setup GET request with bounding boxes
                var lines = linesWithHeights.Select(item => new
                {
                    text = item.text,
                    boundingBox = new[] { 0.0, 0.0, 100.0, 0.0, 100.0, item.height, 0.0, item.height }
                }).ToList();

                var resultJson = $$"""
                {
                    "status": "{{operationStatus}}",
                    "analyzeResult": {
                        "readResults": [
                            {
                                "lines": {{System.Text.Json.JsonSerializer.Serialize(lines)}}
                            }
                        ]
                    }
                }
                """;

                mockHandler
                    .Protected()
                    .Setup<Task<HttpResponseMessage>>(
                        "SendAsync",
                        ItExpr.Is<HttpRequestMessage>(req =>
                            req.Method == HttpMethod.Get &&
                            req.RequestUri!.ToString() == operationUrl),
                        ItExpr.IsAny<CancellationToken>()
                    )
                    .ReturnsAsync(new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(resultJson)
                    });

                return mockHandler.Object;
            }

            [Fact]
            public async Task ExtractFilteredText_WithWordCountWithinRange_ReturnsUnchanged()
            {
                // Arrange - 6 words total (within 3-15 range)
                var linesWithHeights = new List<(string text, double height)>
                {
                    ("The Great Gatsby", 50.0),      // 3 words
                    ("F. Scott Fitzgerald", 40.0)     // 3 words
                };

                var mockHandler = CreateMockHandlerWithBoundingBoxes(linesWithHeights);
                var httpClient = new HttpClient(mockHandler);
                var service = new AzureVisionService(httpClient, Configuration, LoggerMock.Object, pollingDelayMs: 0);

                var imageBytes = CreateValidJpegImage();
                using var stream = new MemoryStream(imageBytes);

                // Act
                var result = await service.AnalyzeCoverImageAsync(stream, "image/jpeg");

                // Assert
                result.IsSuccess.Should().BeTrue();
                result.FilteredText.Should().HaveCount(6); // Now individual words
                var words = result.FilteredText.Select(w => w.Text).ToList();
                words.Should().Contain("The");
                words.Should().Contain("Great");
                words.Should().Contain("Gatsby");
                words.Should().Contain("Fitzgerald");

                // Verify word count is within range
                result.FilteredText.Count.Should().BeGreaterThanOrEqualTo(3);
                result.FilteredText.Count.Should().BeLessThanOrEqualTo(15);
            }

            [Fact]
            public async Task ExtractFilteredText_WithTooManyWords_TrimsToMaximum()
            {
                // Arrange - 25 words total (exceeds 15 word limit)
                var linesWithHeights = new List<(string text, double height)>
                {
                    ("The Great Gatsby", 50.0),                           // 3 words (large)
                    ("A Novel of the Jazz Age", 45.0),                    // 6 words (large)
                    ("by F. Scott Fitzgerald", 40.0),                     // 4 words (large)
                    ("New York Times Bestselling Author", 20.0),          // 5 words (small)
                    ("Now a Major Motion Picture", 15.0),                 // 5 words (small)
                    ("Anniversary Edition", 10.0)                         // 2 words (smallest)
                };

                var mockHandler = CreateMockHandlerWithBoundingBoxes(linesWithHeights);
                var httpClient = new HttpClient(mockHandler);
                var service = new AzureVisionService(httpClient, Configuration, LoggerMock.Object, pollingDelayMs: 0);

                var imageBytes = CreateValidJpegImage();
                using var stream = new MemoryStream(imageBytes);

                // Act
                var result = await service.AnalyzeCoverImageAsync(stream, "image/jpeg");

                // Assert
                result.IsSuccess.Should().BeTrue();

                // Should keep largest words (up to 15 total)
                var words = result.FilteredText.Select(w => w.Text).ToList();
                words.Should().Contain("Gatsby");
                words.Should().Contain("Novel");
                words.Should().Contain("Fitzgerald");

                // Should drop smallest text
                words.Should().NotContain("Anniversary");
                words.Should().NotContain("Edition");

                // Total words should be <= 15
                result.FilteredText.Count.Should().BeLessThanOrEqualTo(15);
            }

            [Fact]
            public async Task ExtractFilteredText_WithTooFewWords_KeepsAllText()
            {
                // Arrange - Only 2 words (below 3 word minimum)
                var linesWithHeights = new List<(string text, double height)>
                {
                    ("Dune", 50.0),           // 1 word
                    ("Herbert", 40.0)         // 1 word
                };

                var mockHandler = CreateMockHandlerWithBoundingBoxes(linesWithHeights);
                var httpClient = new HttpClient(mockHandler);
                var service = new AzureVisionService(httpClient, Configuration, LoggerMock.Object, pollingDelayMs: 0);

                var imageBytes = CreateValidJpegImage();
                using var stream = new MemoryStream(imageBytes);

                // Act
                var result = await service.AnalyzeCoverImageAsync(stream, "image/jpeg");

                // Assert
                result.IsSuccess.Should().BeTrue();
                result.FilteredText.Should().HaveCount(2);
                var words = result.FilteredText.Select(w => w.Text).ToList();
                words.Should().Contain("Dune");
                words.Should().Contain("Herbert");
            }

            [Fact]
            public async Task ExtractFilteredText_PreservesOriginalOrderAfterTrimming()
            {
                // Arrange - Words should be kept in original OCR order, not size order
                var linesWithHeights = new List<(string text, double height)>
                {
                    ("The Hobbit", 50.0),                     // 2 words (line 1, large)
                    ("An Unexpected Journey", 30.0),          // 3 words (line 2, medium)
                    ("by J.R.R. Tolkien", 45.0),              // 3 words (line 3, large)
                    ("Classic Fantasy Novel", 25.0),          // 3 words (line 4, medium)
                    ("Anniversary Edition", 20.0),            // 2 words (line 5, small)
                    ("With New Introduction", 15.0)           // 3 words (line 6, smallest)
                };

                var mockHandler = CreateMockHandlerWithBoundingBoxes(linesWithHeights);
                var httpClient = new HttpClient(mockHandler);
                var service = new AzureVisionService(httpClient, Configuration, LoggerMock.Object, pollingDelayMs: 0);

                var imageBytes = CreateValidJpegImage();
                using var stream = new MemoryStream(imageBytes);

                // Act
                var result = await service.AnalyzeCoverImageAsync(stream, "image/jpeg");

                // Assert
                result.IsSuccess.Should().BeTrue();

                // Verify results are in original order (not sorted by size)
                var words = result.FilteredText.Select(w => w.Text).ToList();
                var indexOfHobbit = words.IndexOf("Hobbit");
                var indexOfTolkien = words.FindIndex(w => w.Contains("Tolkien"));

                if (indexOfHobbit >= 0 && indexOfTolkien >= 0)
                {
                    indexOfHobbit.Should().BeLessThan(indexOfTolkien,
                        "original OCR order should be preserved");
                }
            }
        }

        public class ConfigurationTests : AzureVisionServiceTestBase
        {
            [Fact]
            public void Constructor_WithMissingEndpoint_ThrowsException()
            {
                // Arrange
                var configData = new Dictionary<string, string?>
                {
                    { "AzureVision:ApiKey", "test-key" }
                    // Missing Endpoint
                };
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(configData)
                    .Build();

                var httpClient = new HttpClient();

                // Act & Assert
                var act = () => new AzureVisionService(httpClient, config, LoggerMock.Object);
                act.Should().Throw<InvalidOperationException>()
                    .WithMessage("*Endpoint not configured*");
            }

            [Fact]
            public void Constructor_WithMissingApiKey_ThrowsException()
            {
                // Arrange
                var configData = new Dictionary<string, string?>
                {
                    { "AzureVision:Endpoint", "https://test.cognitiveservices.azure.com" }
                    // Missing ApiKey
                };
                var config = new ConfigurationBuilder()
                    .AddInMemoryCollection(configData)
                    .Build();

                var httpClient = new HttpClient();

                // Act & Assert
                var act = () => new AzureVisionService(httpClient, config, LoggerMock.Object);
                act.Should().Throw<InvalidOperationException>()
                    .WithMessage("*ApiKey not configured*");
            }
        }
    }
}
