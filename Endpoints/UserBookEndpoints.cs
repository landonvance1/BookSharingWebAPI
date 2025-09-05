using BookSharingApp.Data;
using BookSharingApp.Models;
using BookSharingApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookSharingApp.Endpoints
{
    public static class UserBookEndpoints
    {
        public static void MapUserBookEndpoints(this WebApplication app)
        {
            var userBooks = app.MapGroup("/user-books").WithTags("UserBooks").RequireAuthorization();

            userBooks.MapGet("/user/{userId}", async (string userId, ApplicationDbContext context) => 
            {
                var userBooks = await context.UserBooks
                    .Include(ub => ub.Book)
                    .Where(ub => ub.UserId == userId)
                    .ToListAsync();
                
                return Results.Ok(userBooks);
            })
            .WithName("GetUserBooks")
            .WithOpenApi();

            userBooks.MapPut("/{userBookId:int}/status", async (int userBookId, [FromBody] int status, HttpContext httpContext, ApplicationDbContext context) => 
            {
                if (!Enum.IsDefined(typeof(BookStatus), status))
                    return Results.BadRequest("Status must be Available (1), OnLoan (2), or Unavailable (3)");
                
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
                
                var userBook = await context.UserBooks.FindAsync(userBookId);
                
                if (userBook is null)
                    return Results.NotFound();
                
                if (userBook.UserId != currentUserId)
                    return Results.Forbid();
                
                userBook.Status = (BookStatus)status;
                await context.SaveChangesAsync();
                
                return Results.Ok(userBook);
            })
            .WithName("UpdateUserBookStatus")
            .WithOpenApi();

            userBooks.MapPost("/", async ([FromBody] int bookId, HttpContext httpContext, ApplicationDbContext context) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
                
                // Check if book exists
                var bookExists = await context.Books.AnyAsync(b => b.Id == bookId);
                if (!bookExists)
                    return Results.BadRequest("Book not found");
                
                // Check if user already has this book
                var existingUserBook = await context.UserBooks
                    .FirstOrDefaultAsync(ub => ub.UserId == currentUserId && ub.BookId == bookId);
                if (existingUserBook != null)
                    return Results.Conflict("User already has this book");
                
                var userBook = new UserBook
                {
                    UserId = currentUserId,
                    BookId = bookId,
                    Status = BookStatus.Available
                };
                
                context.UserBooks.Add(userBook);
                await context.SaveChangesAsync();
                
                return Results.Created($"/user-books/{userBook.Id}", userBook);
            })
            .WithName("AddUserBook")
            .WithOpenApi();

            userBooks.MapDelete("/{userBookId:int}", async (int userBookId, HttpContext httpContext, ApplicationDbContext context) => 
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
                
                var userBook = await context.UserBooks.FindAsync(userBookId);
                
                if (userBook is null)
                    return Results.NotFound();
                
                if (userBook.UserId != currentUserId)
                    return Results.Forbid();
                
                context.UserBooks.Remove(userBook);
                await context.SaveChangesAsync();
                
                return Results.NoContent();
            })
            .WithName("DeleteUserBook")
            .WithOpenApi();

            userBooks.MapGet("/search", async (string? search, HttpContext httpContext, ApplicationDbContext context) => 
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
            .WithName("SearchUserBooks")
            .WithOpenApi();
        }
    }

}