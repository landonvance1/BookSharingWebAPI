using BookSharingApp.Data;

namespace BookSharingApp.Models
{
    public interface IChatContext
    {
        int ThreadId { get; }
        Task<bool> CanUserAccessAsync(string userId, ApplicationDbContext context);
        Task<IEnumerable<string>> GetAuthorizedUserIdsAsync(ApplicationDbContext context);
        Task<ChatThread?> GetThreadAsync(ApplicationDbContext context);
    }
}