using BookSharingApp.Data;
using BookSharingApp.Models;

namespace BookSharingApp.Endpoints
{
    public static class BookEndpoints
    {
        public static void MapBookEndpoints(this WebApplication app)
        {
            app.MapGet("/books", (MockDatabase db) => db.GetAllBooks())
               .WithName("GetAllBooks")
               .WithOpenApi();

            app.MapGet("/books/{id:int}", (int id, MockDatabase db) => 
            {
                var book = db.GetBookById(id);
                return book is not null ? Results.Ok(book) : Results.NotFound();
            })
            .WithName("GetBookById")
            .WithOpenApi();

            app.MapPost("/books", (Book book, MockDatabase db) => 
            {
                var createdBook = db.AddBook(book);
                return Results.Created($"/books/{createdBook.Id}", createdBook);
            })
            .WithName("AddBook")
            .WithOpenApi();

            app.MapGet("/books/search", (string? title, string? author, MockDatabase db) => 
                db.SearchBooks(title, author))
               .WithName("SearchBooks")
               .WithOpenApi();
        }
    }
}