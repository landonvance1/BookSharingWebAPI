using BookSharingApp.Models;

namespace BookSharingApp.Services
{
    public interface IChatService
    {
        Task CreateShareChatAsync(int shareId);
        Task SendSystemMessageAsync(int shareId, string message);
    }
}