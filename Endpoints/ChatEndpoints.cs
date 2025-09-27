using BookSharingApp.Data;
using BookSharingApp.Models;
using BookSharingApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookSharingApp.Endpoints
{
    public static class ChatEndpoints
    {
        public static void MapChatEndpoints(this WebApplication app)
        {
            var chats = app.MapGroup("/shares/{shareId:int}/chat").WithTags("Chat").RequireAuthorization();

            // GET /shares/{shareId}/chat/messages - Get paginated chat messages
            chats.MapGet("/messages", async (int shareId, HttpContext httpContext, ApplicationDbContext context,
                [FromQuery] int page = 1, [FromQuery] int pageSize = 50) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                // Validate page parameters
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                // Find the share chat thread
                var shareChatThread = await context.ShareChatThreads
                    .Include(sct => sct.Share)
                        .ThenInclude(s => s.UserBook)
                    .FirstOrDefaultAsync(sct => sct.ShareId == shareId);

                if (shareChatThread?.Share == null)
                    return Results.NotFound("Share not found");

                // Check access permissions
                var share = shareChatThread.Share;
                if (share.Borrower != currentUserId && share.UserBook.UserId != currentUserId)
                    return Results.Forbid();

                // Get paginated messages
                var messages = await context.ChatMessages
                    .Include(m => m.Sender)
                    .Where(m => m.ThreadId == shareChatThread.ThreadId)
                    .OrderByDescending(m => m.SentAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(m => new
                    {
                        m.Id,
                        m.Content,
                        m.SentAt,
                        m.IsSystemMessage,
                        Sender = new
                        {
                            m.Sender.Id,
                            m.Sender.FirstName,
                            m.Sender.LastName
                        }
                    })
                    .ToListAsync();

                var totalMessages = await context.ChatMessages
                    .CountAsync(m => m.ThreadId == shareChatThread.ThreadId);

                var result = new
                {
                    Messages = messages,
                    Pagination = new
                    {
                        Page = page,
                        PageSize = pageSize,
                        TotalMessages = totalMessages,
                        TotalPages = (int)Math.Ceiling(totalMessages / (double)pageSize)
                    }
                };

                return Results.Ok(result);
            })
            .WithName("GetShareChatMessages")
            .WithOpenApi();

            // POST /shares/{shareId}/chat/messages - Send a message (also broadcasts via SignalR)
            chats.MapPost("/messages", async (int shareId, [FromBody] SendMessageRequest request,
                HttpContext httpContext, ApplicationDbContext context) =>
            {
                var currentUserId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

                // Validate request
                if (string.IsNullOrWhiteSpace(request.Content) || request.Content.Length > 2000)
                    return Results.BadRequest("Message content is required and must be less than 2000 characters");

                try
                {
                    var chatContext = await ShareChatContext.CreateForShareAsync(shareId, context);
                    if (chatContext == null || !await chatContext.CanUserAccessAsync(currentUserId, context))
                    {
                        return Results.NotFound("Share not found or access denied");
                    }

                    var message = new ChatMessage
                    {
                        ThreadId = chatContext.ThreadId,
                        SenderId = currentUserId,
                        Content = request.Content.Trim(),
                        SentAt = DateTime.UtcNow,
                        IsSystemMessage = false
                    };

                    context.ChatMessages.Add(message);

                    // Update thread last activity
                    var thread = await context.ChatThreads.FindAsync(chatContext.ThreadId);
                    if (thread != null)
                    {
                        thread.LastActivity = DateTime.UtcNow;
                    }

                    await context.SaveChangesAsync();

                    // Load sender info for response
                    await context.Entry(message).Reference(m => m.Sender).LoadAsync();

                    var messageResponse = new
                    {
                        message.Id,
                        message.Content,
                        message.SentAt,
                        message.IsSystemMessage,
                        Sender = new
                        {
                            message.Sender.Id,
                            message.Sender.FirstName,
                            message.Sender.LastName
                        }
                    };

                    return Results.Created($"/shares/{shareId}/chat/messages/{message.Id}", messageResponse);
                }
                catch (Exception)
                {
                    // Log error if needed
                    return Results.Problem("Failed to send message");
                }
            })
            .WithName("SendChatMessage")
            .WithOpenApi()
            .WithMetadata(new RateLimitAttribute(RateLimitNames.ChatSend, RateLimitScope.User));
        }
    }

    public record SendMessageRequest(string Content);
}