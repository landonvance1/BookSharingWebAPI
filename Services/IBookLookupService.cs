namespace BookSharingApp.Services
{
    public interface IBookLookupService
    {
        Task<BookLookupResult?> GetBookByIsbnAsync(string isbn);
        Task<List<BookLookupResult>> SearchBooksAsync(string? isbn = null, string? title = null, string? author = null);
        Task<List<BookLookupResult>> SearchBooksByTextAsync(string searchText);
    }

    public class BookLookupResult
    {
        public string Title { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Isbn { get; set; } = string.Empty;
        public string? ThumbnailUrl { get; set; }
    }
}