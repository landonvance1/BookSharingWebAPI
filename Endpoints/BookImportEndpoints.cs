using BookSharingApp.Data;
using BookSharingApp.Helpers;
using BookSharingApp.Models;
using BookSharingApp.Services;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Endpoints
{
    public static class BookImportEndpoints
    {
        public static void MapBookImportEndpoints(this WebApplication app)
        {
            var imports = app.MapGroup("/import/books").WithTags("Book Import").RequireAuthorization();

            imports.MapPut("/isbn/{isbn}", async (string isbn, ApplicationDbContext context, IBookLookupService bookLookupService, IWebHostEnvironment environment) => 
            {
                var existingBook = await context.Books.FirstOrDefaultAsync(b => b.ISBN == isbn);
                if (existingBook != null)
                {
                    return Results.Ok(existingBook);
                }

                var bookData = await bookLookupService.GetBookByIsbnAsync(isbn);
                if (bookData == null)
                {
                    return Results.NotFound($"No book found with ISBN: {isbn}");
                }

                var newBook = new Book
                {
                    Title = bookData.Title,
                    Author = bookData.Author,
                    ISBN = isbn
                };

                // Download and save thumbnail if available
                if (!string.IsNullOrEmpty(bookData.ThumbnailUrl))
                {
                    await ImageHelper.DownloadThumbnailAsync(bookData.ThumbnailUrl, isbn, environment);
                }

                try
                {
                    context.Books.Add(newBook);
                    await context.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // If we get here, it means the book was created between our check and our insert
                    var duplicateBook = await context.Books.FirstOrDefaultAsync(b => b.ISBN == isbn);
                    return duplicateBook != null ? Results.Ok(duplicateBook) : Results.Conflict($"A book with ISBN '{isbn}' already exists.");
                }

                return Results.Ok(newBook);
            })
            .WithName("ImportBookByISBN")
            .WithOpenApi();
        }
    }
}