using System.Text.Json;
using System.Text.Json.Serialization;

namespace BookSharingApp.Services
{
    public class OpenLibraryService : IBookLookupService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<OpenLibraryService> _logger;

        public OpenLibraryService(HttpClient httpClient, ILogger<OpenLibraryService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<BookLookupResult?> GetBookByIsbnAsync(string isbn)
        {
            try
            {
                var cleanIsbn = CleanIsbn(isbn);
                var response = await _httpClient.GetAsync($"https://openlibrary.org/search.json?isbn={cleanIsbn}");
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("OpenLibrary API returned {StatusCode} for ISBN {ISBN}", response.StatusCode, cleanIsbn);
                    return null;
                }

                var jsonContent = await response.Content.ReadAsStringAsync();
                var searchResult = JsonSerializer.Deserialize<OpenLibrarySearchResponse>(jsonContent, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                });

                if (searchResult?.Docs?.Any() != true)
                {
                    _logger.LogInformation("No books found for ISBN {ISBN}", cleanIsbn);
                    return null;
                }

                var book = searchResult.Docs.First();
                return new BookLookupResult
                {
                    Title = book.Title ?? "Unknown Title",
                    Author = book.AuthorName?.FirstOrDefault() ?? "Unknown Author",
                    Isbn = cleanIsbn,
                    ThumbnailUrl = book.CoverId != null ? $"https://covers.openlibrary.org/b/id/{book.CoverId}-M.jpg" : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching book data from OpenLibrary for ISBN {ISBN}", isbn);
                return null;
            }
        }

        private static string CleanIsbn(string isbn)
        {
            return isbn.Replace("-", "").Replace(" ", "");
        }
    }

    internal class OpenLibrarySearchResponse
    {
        public int NumFound { get; set; }
        public int Start { get; set; }
        public List<OpenLibraryBook>? Docs { get; set; }
    }

    internal class OpenLibraryBook
    {
        public string? Title { get; set; }
        public List<string>? AuthorName { get; set; }
        public List<string>? Isbn { get; set; }
        
        [JsonPropertyName("cover_i")]
        public int? CoverId { get; set; }
        
        public int? FirstPublishYear { get; set; }
    }
}