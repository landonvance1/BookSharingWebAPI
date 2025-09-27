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
    }
}