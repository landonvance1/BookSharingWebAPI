using BookSharingApp.Models;
using BookSharingApp.Common;
using BookSharingApp.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookSharingApp.Endpoints
{
    public static class ShareEndpoints
    {
        public static void MapShareEndpoints(this WebApplication app)
        {
            var shares = app.MapGroup("/shares").WithTags("Shares").RequireAuthorization();

            // POST /shares?userbookid={id} - Create a new share request
            shares.MapPost("/", async (int userbookid, HttpContext httpContext, IShareService shareService, IChatService chatService) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                try
                {
                    var share = await shareService.CreateShareAsync(userbookid, currentUserId);

                    // Create chat thread for the share
                    try
                    {
                        await chatService.CreateShareChatAsync(share.Id);
                    }
                    catch (Exception)
                    {
                        // Log error but don't fail the share creation
                        // The chat can be created later if needed
                    }

                    return Results.Created($"/shares/{share.Id}", share);
                }
                catch (InvalidOperationException ex)
                {
                    return ex.Message.Contains("already have an active share")
                        ? Results.Conflict(ex.Message)
                        : Results.BadRequest(ex.Message);
                }
            })
            .WithName("CreateShare")
            .WithOpenApi();

            // GET /shares/borrower - Get shares where current user is the borrower
            shares.MapGet("/borrower", async (HttpContext httpContext, IShareService shareService) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
                var borrowerShares = await shareService.GetBorrowerSharesAsync(currentUserId);
                return Results.Ok(borrowerShares);
            })
            .WithName("GetBorrowerShares")
            .WithOpenApi();

            // GET /shares/lender - Get shares where current user is the lender
            shares.MapGet("/lender", async (HttpContext httpContext, IShareService shareService) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
                var lenderShares = await shareService.GetLenderSharesAsync(currentUserId);
                return Results.Ok(lenderShares);
            })
            .WithName("GetLenderShares")
            .WithOpenApi();

            // GET /shares/borrower/archived - Get archived shares where current user is the borrower
            shares.MapGet("/borrower/archived", async (HttpContext httpContext, IShareService shareService) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
                var borrowerShares = await shareService.GetArchivedBorrowerSharesAsync(currentUserId);
                return Results.Ok(borrowerShares);
            })
            .WithName("GetArchivedBorrowerShares")
            .WithOpenApi();

            // GET /shares/lender/archived - Get archived shares where current user is the lender
            shares.MapGet("/lender/archived", async (HttpContext httpContext, IShareService shareService) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
                var lenderShares = await shareService.GetArchivedLenderSharesAsync(currentUserId);
                return Results.Ok(lenderShares);
            })
            .WithName("GetArchivedLenderShares")
            .WithOpenApi();

            // PUT /shares/{id}/status - Update share status
            shares.MapPut("/{id}/status", async (int id, [FromBody] ShareStatusUpdateRequest request,
                HttpContext httpContext, IShareService shareService, IChatService chatService) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                try
                {
                    // Update the status (validation happens in service layer)
                    await shareService.UpdateShareStatusAsync(id, request.Status, currentUserId);

                    // Send system message for status change
                    try
                    {
                        var statusMessage = GetStatusChangeMessage(request.Status);
                        if (!string.IsNullOrEmpty(statusMessage))
                        {
                            await chatService.SendSystemMessageAsync(id, statusMessage);
                        }
                    }
                    catch (Exception)
                    {
                        // Log error but don't fail the status update
                    }

                    // Get updated share to return
                    var updatedShare = await shareService.GetShareAsync(id);
                    return Results.Ok(updatedShare);
                }
                catch (InvalidOperationException ex)
                {
                    // Return appropriate HTTP status based on error
                    return ex.Message == "Share not found"
                        ? Results.NotFound(ex.Message)
                        : Results.BadRequest(ex.Message);
                }
            })
            .WithName("UpdateShareStatus")
            .WithOpenApi();

            // PUT /shares/{id}/return-date - Set return date (lender only)
            shares.MapPut("/{id}/return-date", async (int id, [FromBody] SetReturnDateRequest request,
                HttpContext httpContext, IShareService shareService) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                try
                {
                    await shareService.UpdateShareDueDateAsync(id, request.ReturnDate, currentUserId);
                    var updatedShare = await shareService.GetShareAsync(id);
                    return Results.Ok(updatedShare);
                }
                catch (InvalidOperationException ex)
                {
                    return Results.NotFound(ex.Message);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
            })
            .WithName("SetReturnDate")
            .WithOpenApi();

            // POST /shares/{id}/archive - Archive a share (terminal status only)
            shares.MapPost("/{id}/archive", async (int id, HttpContext httpContext, IShareService shareService) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                try
                {
                    await shareService.ArchiveShareAsync(id, currentUserId);
                    return Results.Ok(new { message = "Share archived successfully" });
                }
                catch (InvalidOperationException ex)
                {
                    return ex.Message.Contains("not found")
                        ? Results.NotFound(ex.Message)
                        : Results.BadRequest(ex.Message);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
            })
            .WithName("ArchiveShare")
            .WithOpenApi();

            // POST /shares/{id}/unarchive - Unarchive a share
            shares.MapPost("/{id}/unarchive", async (int id, HttpContext httpContext, IShareService shareService) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                try
                {
                    await shareService.UnarchiveShareAsync(id, currentUserId);
                    return Results.Ok(new { message = "Share unarchived successfully" });
                }
                catch (InvalidOperationException ex)
                {
                    return ex.Message.Contains("not found")
                        ? Results.NotFound(ex.Message)
                        : Results.BadRequest(ex.Message);
                }
                catch (UnauthorizedAccessException)
                {
                    return Results.Unauthorized();
                }
            })
            .WithName("UnarchiveShare")
            .WithOpenApi();
        }

        private static string GetStatusChangeMessage(ShareStatus newStatus)
        {
            return newStatus switch
            {
                ShareStatus.Ready => "📚 Book is ready for pickup! You can coordinate the details here.",
                ShareStatus.PickedUp => "✅ Book has been picked up. Enjoy your reading!",
                ShareStatus.Returned => "📖 Book has been returned. Please confirm if everything looks good.",
                ShareStatus.HomeSafe => "🏠 Share completed successfully! Thank you for using the book sharing community.",
                ShareStatus.Disputed => "⚠️ A dispute has been raised. Please discuss the issue here.",
                _ => string.Empty
            };
        }
    }

    public record ShareStatusUpdateRequest(ShareStatus Status);
    public record SetReturnDateRequest(DateTime ReturnDate);
}