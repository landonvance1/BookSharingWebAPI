using BookSharingApp.Data;
using BookSharingApp.Models;
using Microsoft.EntityFrameworkCore;

namespace BookSharingApp.Services
{
    public class ChatService : IChatService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatService> _logger;

        public ChatService(ApplicationDbContext context, ILogger<ChatService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Chat thread management
        public async Task CreateShareChatAsync(int shareId)
        {
            try
            {
                // Check if chat already exists
                var existingChat = await _context.ShareChatThreads
                    .FirstOrDefaultAsync(sct => sct.ShareId == shareId);

                if (existingChat != null)
                {
                    _logger.LogWarning("Chat thread already exists for share {ShareId}", shareId);
                    return;
                }

                // Create new chat thread
                var thread = new ChatThread
                {
                    CreatedAt = DateTime.UtcNow,
                    LastActivity = DateTime.UtcNow
                };

                _context.ChatThreads.Add(thread);
                await _context.SaveChangesAsync();

                // Create share chat thread relationship
                var shareChatThread = new ShareChatThread
                {
                    ThreadId = thread.Id,
                    ShareId = shareId
                };

                _context.ShareChatThreads.Add(shareChatThread);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Created chat thread {ThreadId} for share {ShareId}", thread.Id, shareId);

                // Send welcome system message
                await SendSystemMessageAsync(shareId, "Chat created! You can now coordinate pickup details and discuss the book.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create chat for share {ShareId}", shareId);
                throw;
            }
        }

        public async Task SendSystemMessageAsync(int shareId, string message)
        {
            try
            {
                var shareChatThread = await _context.ShareChatThreads
                    .FirstOrDefaultAsync(sct => sct.ShareId == shareId);

                if (shareChatThread == null)
                {
                    _logger.LogWarning("No chat thread found for share {ShareId}", shareId);
                    return;
                }

                var systemMessage = new ChatMessage
                {
                    ThreadId = shareChatThread.ThreadId,
                    SenderId = "system", // Use a special system user ID
                    Content = message,
                    SentAt = DateTime.UtcNow,
                    IsSystemMessage = true
                };

                _context.ChatMessages.Add(systemMessage);

                // Update thread last activity
                var thread = await _context.ChatThreads.FindAsync(shareChatThread.ThreadId);
                if (thread != null)
                {
                    thread.LastActivity = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Sent system message to share {ShareId} chat", shareId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send system message to share {ShareId}", shareId);
                throw;
            }
        }

        // Message operations
        public async Task<List<ChatMessage>> GetMessageThreadAsync(int shareId, int page, int pageSize)
        {
            // Validate page parameters
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            // Find the share chat thread
            var shareChatThread = await _context.ShareChatThreads
                .FirstOrDefaultAsync(sct => sct.ShareId == shareId);

            if (shareChatThread == null)
                throw new InvalidOperationException("Share chat thread not found");

            // Get paginated messages
            var messages = await _context.ChatMessages
                .Include(m => m.Sender)
                .Where(m => m.ThreadId == shareChatThread.ThreadId)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return messages;
        }

        public async Task<int> GetMessageCountAsync(int shareId)
        {
            var shareChatThread = await _context.ShareChatThreads
                .FirstOrDefaultAsync(sct => sct.ShareId == shareId);

            if (shareChatThread == null)
                return 0;

            return await _context.ChatMessages
                .CountAsync(m => m.ThreadId == shareChatThread.ThreadId);
        }

        public async Task<ChatMessage?> GetMessageAsync(int messageId)
        {
            return await _context.ChatMessages
                .Include(m => m.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId);
        }

        public async Task<ChatMessage> SendMessageAsync(int shareId, string senderId, string content)
        {
            // Validate message content
            if (string.IsNullOrWhiteSpace(content) || content.Length > 2000)
                throw new ArgumentException("Message content is required and must be less than 2000 characters");

            var chatContext = await ShareChatContext.CreateForShareAsync(shareId, _context);
            if (chatContext == null)
                throw new InvalidOperationException("Share not found");

            if (!await chatContext.CanUserAccessAsync(senderId, _context))
                throw new UnauthorizedAccessException("Access denied to this share chat");

            var message = new ChatMessage
            {
                ThreadId = chatContext.ThreadId,
                SenderId = senderId,
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

            // Load sender info for response
            await _context.Entry(message).Reference(m => m.Sender).LoadAsync();

            _logger.LogInformation("Message {MessageId} sent in share {ShareId} by user {SenderId}",
                message.Id, shareId, senderId);

            return message;
        }

        public async Task DeleteMessageAsync(int messageId)
        {
            var message = await _context.ChatMessages.FindAsync(messageId);
            if (message == null)
                throw new InvalidOperationException("Message not found");

            _context.ChatMessages.Remove(message);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Deleted message {MessageId}", messageId);
        }
    }
}