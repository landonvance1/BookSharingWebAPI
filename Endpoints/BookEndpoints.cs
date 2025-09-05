using BookSharingApp.Data;
using BookSharingApp.Models;
using BookSharingApp.Services;
using BookSharingApp.Common;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Endpoints
{
    public static class BookEndpoints
    {
        public static void MapBookEndpoints(this WebApplication app)
        {
            var books = app.MapGroup("/books").WithTags("Books").RequireAuthorization();

            books.MapGet("/", async (ApplicationDbContext context) => 
                await context.Books.ToListAsync())
               .WithName("GetAllBooks")
               .WithOpenApi();

            books.MapGet("/{id:int}", async (int id, ApplicationDbContext context) => 
            {
                var book = await context.Books.FindAsync(id);
                return book is not null ? Results.Ok(book) : Results.NotFound();
            })
            .WithName("GetBookById")
            .WithOpenApi();

            books.MapPost("/", async (Book book, ApplicationDbContext context) => 
            {
                // Check if ISBN already exists
                var existingBook = await context.Books.FirstOrDefaultAsync(b => b.ISBN == book.ISBN);
                if (existingBook != null)
                {
                    return Results.Conflict($"A book with ISBN '{book.ISBN}' already exists.");
                }

                book.Id = 0; // Ensure EF generates new ID
                context.Books.Add(book);
                await context.SaveChangesAsync();
                return Results.Created($"/books/{book.Id}", book);
            })
            .WithName("AddBook")
            .WithOpenApi();

            books.MapGet("/search", async (string? isbn, string? title, string? author, bool includeExternal, ApplicationDbContext context, IBookLookupService bookLookupService) => 
            {
                var results = new List<Book>();
                
                // Search local database first
                var query = context.Books.AsQueryable();
                
                if (!string.IsNullOrWhiteSpace(isbn))
                    query = query.Where(b => b.ISBN.Contains(isbn));
                
                if (!string.IsNullOrWhiteSpace(title))
                    query = query.Where(b => b.Title.Contains(title));
                
                if (!string.IsNullOrWhiteSpace(author))
                    query = query.Where(b => b.Author.Contains(author));
                
                var localResults = await query.ToListAsync();
                results.AddRange(localResults);
                
                // If includeExternal is true, search OpenLibrary
                if (includeExternal)
                {
                    var externalResults = await bookLookupService.SearchBooksAsync(isbn, title, author);
                    
                    // Convert external results to Book objects and merge
                    foreach (var externalBook in externalResults)
                    {
                        // Skip if we already have this book locally (by title and author)
                        bool isDuplicate = localResults.Any(b => 
                            string.Equals(NormalizeString(b.Title), NormalizeString(externalBook.Title), StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(NormalizeString(b.Author), NormalizeString(externalBook.Author), StringComparison.OrdinalIgnoreCase));
                        
                        if (isDuplicate)
                            continue;
                        
                        // Add external book with negative ID
                        results.Add(new Book
                        {
                            Id = -results.Count - 1,
                            Title = externalBook.Title,
                            Author = externalBook.Author,
                            ISBN = externalBook.Isbn,
                            ExternalThumbnailUrl = externalBook.ThumbnailUrl
                        });
                    }
                }
                
                // Check if we have too many results (more than 20)
                if (results.Count > Constants.MaxExternalSearchResults)
                {
                    return Results.Ok(new { 
                        books = results.Take(Constants.MaxExternalSearchResults).ToList(),
                        hasMore = true,
                        message = "Too many results. Please use more specific filters."
                    });
                }
                
                return Results.Ok(new { 
                    books = results,
                    hasMore = false 
                });
            })
            .WithName("SearchBooks")
            .WithOpenApi();

        }

        private static string NormalizeString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;
            
            return input.Trim().ToLowerInvariant();
        }

    }
}