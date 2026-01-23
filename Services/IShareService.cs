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
        Task RaiseDisputeAsync(int shareId, string raisedByUserId);
        Task ArchiveShareAsync(int shareId, string archivedByUserId);
        Task UnarchiveShareAsync(int shareId, string unarchivedByUserId);

        /// <summary>
        /// Handles shares when a UserBook is soft-deleted.
        /// - Requested shares: Auto-decline with borrower notification
        /// - Active shares (Ready/PickedUp/Returned): Auto-archive for owner, notify borrower
        /// - Disputed shares: Silently auto-archive for owner
        /// - Terminal shares (HomeSafe/Declined): Auto-archive for owner
        /// </summary>
        /// <param name="userBookId">The UserBook being deleted</param>
        /// <param name="lenderId">The owner/lender of the UserBook</param>
        /// <returns>True if there were active shares that required handling</returns>
        Task<bool> HandleUserBookDeletionAsync(int userBookId, string lenderId);
    }
}
