using BookSharingApp.Common;
using BookSharingApp.Models;

namespace BookSharingApp.Services
{
    public interface IShareService
    {
        // Reads - simple pass-through to EF
        Task<Share?> GetShareAsync(int id);
        Task<List<Share>> GetBorrowerSharesAsync(string userId);
        Task<List<Share>> GetLenderSharesAsync(string userId);
        Task<List<Share>> GetArchivedBorrowerSharesAsync(string userId);
        Task<List<Share>> GetArchivedLenderSharesAsync(string userId);

        // Writes - intent-based methods with business logic
        Task<Share> CreateShareAsync(int userBookId, string borrowerId);
        Task UpdateShareStatusAsync(int shareId, ShareStatus newStatus, string updatedByUserId);
        Task UpdateShareDueDateAsync(int shareId, DateTime newDueDate, string updatedByUserId);
        Task ArchiveShareAsync(int shareId, string archivedByUserId);
        Task UnarchiveShareAsync(int shareId, string unarchivedByUserId);
    }
}
