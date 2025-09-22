using BookSharingApp.Data;
using BookSharingApp.Models;
using BookSharingApp.Common;
using BookSharingApp.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookSharingApp.Endpoints
{
    public static class ShareEndpoints
    {
        public static void MapShareEndpoints(this WebApplication app)
        {
            var shares = app.MapGroup("/shares").WithTags("Shares").RequireAuthorization();

            // POST /shares?userbookid={id} - Create a new share request
            shares.MapPost("/", async (int userbookid, HttpContext httpContext, ApplicationDbContext context) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
                
                // Check if userbook exists
                var userBook = await context.UserBooks.FindAsync(userbookid);
                if (userBook is null)
                    return Results.BadRequest("UserBook not found");
                
                // Check if user is trying to borrow their own book
                if (userBook.UserId == currentUserId)
                    return Results.BadRequest("Cannot borrow your own book");
                
                // Check if userbook is available
                if (userBook.Status != UserBookStatus.Available)
                    return Results.BadRequest("Book is not available for sharing");
                
                // Check if borrower and lender share at least one community
                var borrowerCommunities = await context.CommunityUsers
                    .Where(cu => cu.UserId == currentUserId)
                    .Select(cu => cu.CommunityId)
                    .ToListAsync();
                
                var lenderCommunities = await context.CommunityUsers
                    .Where(cu => cu.UserId == userBook.UserId)
                    .Select(cu => cu.CommunityId)
                    .ToListAsync();
                
                var sharedCommunities = borrowerCommunities.Intersect(lenderCommunities).Any();
                if (!sharedCommunities)
                    return Results.BadRequest("You must share a community with the book owner to request this book");
                
                // Check if there's already an active share for this userbook by this borrower
                var existingShare = await context.Shares
                    .FirstOrDefaultAsync(s => s.UserBookId == userbookid && 
                                            s.Borrower == currentUserId && 
                                            s.Status <= ShareStatus.Returned);
                if (existingShare != null)
                    return Results.Conflict("You already have an active share request for this book");
                
                var share = new Share
                {
                    UserBookId = userbookid,
                    Borrower = currentUserId,
                    ReturnDate = null,
                    Status = ShareStatus.Requested
                };
                
                context.Shares.Add(share);
                await context.SaveChangesAsync();
                
                return Results.Created($"/shares/{share.Id}", share);
            })
            .WithName("CreateShare")
            .WithOpenApi();

            // GET /shares/borrower - Get shares where current user is the borrower
            shares.MapGet("/borrower", async (HttpContext httpContext, ApplicationDbContext context) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                List<Share> borrowerShares = await context.Shares
                    .Include(s => s.UserBook)
                        .ThenInclude(ub => ub.Book)
                    .Include(s => s.UserBook)
                        .ThenInclude(ub => ub.User)
                    .Where(s => s.Borrower == currentUserId && s.Status <= ShareStatus.Returned)
                    .ToListAsync();

                return Results.Ok(borrowerShares);
            })
            .WithName("GetBorrowerShares")
            .WithOpenApi();

            // GET /shares/lender - Get shares where current user is the lender
            shares.MapGet("/lender", async (HttpContext httpContext, ApplicationDbContext context) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                List<Share> lenderShares = await context.Shares
                    .Include(s => s.UserBook)
                        .ThenInclude(ub => ub.Book)
                    .Include(s => s.BorrowerUser)
                    .Where(s => s.UserBook.UserId == currentUserId && s.Status <= ShareStatus.Returned)
                    .ToListAsync();

                return Results.Ok(lenderShares);
            })
            .WithName("GetLenderShares")
            .WithOpenApi();

            // PUT /shares/{id}/status - Update share status
            shares.MapPut("/{id}/status", async (int id, [FromBody] ShareStatusUpdateRequest request,
                HttpContext httpContext, ApplicationDbContext context) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                // Find the share with required includes
                var share = await context.Shares
                    .Include(s => s.UserBook)
                        .ThenInclude(ub => ub.Book)
                    .Include(s => s.UserBook)
                        .ThenInclude(ub => ub.User)
                    .Include(s => s.BorrowerUser)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (share is null)
                    return Results.NotFound("Share not found");

                // Validate the status transition
                var validator = new ShareStatusValidator();
                var validationResult = validator.ValidateStatusTransition(share, request.Status, currentUserId);

                if (!validationResult.IsValid)
                    return Results.BadRequest(validationResult.ErrorMessage);

                // Update the status
                share.Status = request.Status;
                await context.SaveChangesAsync();

                return Results.Ok(share);
            })
            .WithName("UpdateShareStatus")
            .WithOpenApi();
        }
    }

    public record ShareStatusUpdateRequest(ShareStatus Status);
}