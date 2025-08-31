using BookSharingApp.Data;
using BookSharingApp.Helpers;
using BookSharingApp.Models;
using BookSharingApp.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

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
                book.Id = 0; // Ensure EF generates new ID
                context.Books.Add(book);
                await context.SaveChangesAsync();
                return Results.Created($"/books/{book.Id}", book);
            })
            .WithName("AddBook")
            .WithOpenApi();

            books.MapGet("/search", async (string? search, HttpContext httpContext, ApplicationDbContext context) => 
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
                
                var results = await context.Database
                    .SqlQueryRaw<SearchBookResult>(
                        "SELECT * FROM search_accessible_books({0}, {1})", 
                        currentUserId, 
                        search ?? string.Empty)
                    .ToListAsync();
                
                return Results.Ok(results);
            })
            .WithName("SearchBooks")
            .WithOpenApi();

            books.MapGet("/isbn/{isbn}", async (string isbn, ApplicationDbContext context, IBookLookupService bookLookupService, IWebHostEnvironment environment) => 
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

                context.Books.Add(newBook);
                await context.SaveChangesAsync();

                return Results.Ok(newBook);
            })
            .WithName("GetBookByISBN")
            .WithOpenApi();
        }

    }
}