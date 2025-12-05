using BookSharingApp.Data;
using BookSharingApp.Models;
using BookSharingApp.Services;
using BookSharingApp.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BookSharingApp.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ApplicationDbContext _context;
        private readonly IRateLimitService _rateLimitService;
        private readonly ILogger<ChatHub> _logger;
        private readonly INotificationService _notificationService;

        public ChatHub(ApplicationDbContext context, IRateLimitService rateLimitService, ILogger<ChatHub> logger, INotificationService notificationService)
        {
            _context = context;
            _rateLimitService = rateLimitService;
            _logger = logger;
            _notificationService = notificationService;
        }

        public async Task JoinShareChat(int shareId)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "Authentication required");
                return;
            }

            try
            {
                var chatContext = await ShareChatContext.CreateForShareAsync(shareId, _context);
                if (chatContext == null)
                {
                    await Clients.Caller.SendAsync("Error", "Share not found");
                    return;
                }

                if (!await chatContext.CanUserAccessAsync(userId, _context))
                {
                    await Clients.Caller.SendAsync("Error", "Access denied");
                    return;
                }

                var groupName = $"share-{shareId}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

                _logger.LogInformation("User {UserId} joined share chat {ShareId}", userId, shareId);
                await Clients.Caller.SendAsync("JoinedChat", shareId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining share chat {ShareId} for user {UserId}", shareId, userId);
                await Clients.Caller.SendAsync("Error", "Failed to join chat");
            }
        }

        public async Task LeaveShareChat(int shareId)
        {
            var groupName = $"share-{shareId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} left share chat {ShareId}", userId, shareId);

            await Clients.Caller.SendAsync("LeftChat", shareId);
        }

        public async Task SendMessage(int shareId, string content)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                await Clients.Caller.SendAsync("Error", "Authentication required");
                return;
            }

            // Apply rate limiting (30 messages per 2 minutes)
            if (!await _rateLimitService.TryConsumeForUserAsync(RateLimitNames.ChatSend, userId))
            {
                await Clients.Caller.SendAsync("Error", "Rate limit exceeded. Please slow down.");
                return;
            }

            // Validate message content
            if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
            {
                await Clients.Caller.SendAsync("Error", "Invalid message content");
                return;
            }

            try
            {
                var chatContext = await ShareChatContext.CreateForShareAsync(shareId, _context);
                if (chatContext == null || !await chatContext.CanUserAccessAsync(userId, _context))
                {
                    await Clients.Caller.SendAsync("Error", "Access denied");
                    return;
                }

                var message = new ChatMessage
                {
                    ThreadId = chatContext.ThreadId,
                    SenderId = userId,
                    Content = content.Trim(),
                    SentAt = DateTime.UtcNow,
                    IsSystemMessage = false
                };

                _context.ChatMessages.Add(message);

                // Update thread last activity
                var thread = await _context.ChatThreads.FindAsync(chatContext.ThreadId);
                if (thread != null)
                {
                    thread.LastActivity = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                // Load message with sender info for broadcasting
                await _context.Entry(message).Reference(m => m.Sender).LoadAsync();

                var messageDto = new
                {
                    Id = message.Id,
                    ThreadId = message.ThreadId,
                    Content = message.Content,
                    SentAt = message.SentAt,
                    IsSystemMessage = message.IsSystemMessage,
                    Sender = new
                    {
                        Id = message.Sender.Id,
                        FirstName = message.Sender.FirstName,
                        LastName = message.Sender.LastName
                    }
                };

                var groupName = $"share-{shareId}";
                await Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);

                _logger.LogInformation("Message sent in share chat {ShareId} by user {UserId}", shareId, userId);

                // Create notification for the other party about new message
                await _notificationService.CreateShareNotificationAsync(
                    shareId,
                    Common.NotificationType.ShareMessageReceived,
                    $"New message from {message.Sender.FullName}",
                    userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message in share chat {ShareId} for user {UserId}", shareId, userId);
                await Clients.Caller.SendAsync("Error", "Failed to send message");
            }
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            _logger.LogInformation("User {UserId} disconnected from chat hub", userId);

            await base.OnDisconnectedAsync(exception);
        }
    }
}