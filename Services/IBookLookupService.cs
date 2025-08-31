namespace BookSharingApp.Services
{
    public interface IBookLookupService
    {
        Task<BookLookupResult?> GetBookByIsbnAsync(string isbn);
    }

    public class BookLookupResult
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Isbn { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
    }
}