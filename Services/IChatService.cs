using BookSharingApp.Models;

namespace BookSharingApp.Services
{
    public interface IChatService
    {
        // Chat thread management
        Task CreateShareChatAsync(int shareId);
        Task SendSystemMessageAsync(int shareId, string message);

        // Message operations
        Task<List<ChatMessage>> GetMessageThreadAsync(int shareId, int page, int pageSize);
        Task<int> GetMessageCountAsync(int shareId);
        Task<ChatMessage?> GetMessageAsync(int messageId);
        Task<ChatMessage> SendMessageAsync(int shareId, string senderId, string content);
        Task DeleteMessageAsync(int messageId);
    }
}