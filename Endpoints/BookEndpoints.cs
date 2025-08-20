using BookSharingApp.Data;
using BookSharingApp.Models;
using Microsoft.AspNetCore.Authorization;
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
                book.Id = 0; // Ensure EF generates new ID
                context.Books.Add(book);
                await context.SaveChangesAsync();
                return Results.Created($"/books/{book.Id}", book);
            })
            .WithName("AddBook")
            .WithOpenApi();

            books.MapGet("/search", async (string? search, ApplicationDbContext context) => 
            {
                var query = context.Books.AsQueryable();
                
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(b => EF.Functions.ILike(b.Title, $"%{search}%") || EF.Functions.ILike(b.Author, $"%{search}%"));
                }
                
                return await query.ToListAsync();
            })
            .WithName("SearchBooks")
            .WithOpenApi();
        }
    }
}