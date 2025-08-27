using BookSharingApp.Data;
using BookSharingApp.Models;
using Microsoft.AspNetCore.Authorization;
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
        }
    }
}